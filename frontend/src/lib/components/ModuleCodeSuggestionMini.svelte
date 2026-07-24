<script lang="ts">
  import { currentlySelectedMods } from "$lib/shared/shared.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import type { TimetablePostTemplate } from "$lib/types/db_raw_types";
  import type { ModSummary } from "$lib/types/mod_summaries";
  import { getFullModInfo } from "$lib/utils/fetch_from_cache";
  import {
    checkModAlreadyAdded,
    createModEntry,
  } from "$lib/utils/format_db_information";
  import { get } from "svelte/store";

  interface ModSuggestionsProp {
    mod: ModSummary;
    semester: number;
    acadYear: string;
    timetable_id: string | undefined;
    timetable_name: string;
    user_favourite_colour: string;
    search_term: string;
  }

  let {
    mod,
    semester,
    acadYear,
    timetable_id,
    timetable_name,
    user_favourite_colour,
    search_term = $bindable(),
  }: ModSuggestionsProp = $props();

  async function addMod() {
    if (!mod.semesters.includes(semester)) return;

    // No timetable id: Create one timetable for them:
    if (!timetable_id) {
      const info_to_post: TimetablePostTemplate = {
        academicYear: acadYear,
        metaData: [],
        name: timetable_name,
        semester: semester,
      };
      await $roomHub?.invoke("CreateTimetable", info_to_post);
    }

    const modFullInfo = await getFullModInfo(mod.moduleCode, acadYear);

    const timeTable = modFullInfo.semesterData.find(
      (sem) => sem.semester == semester,
    )!.timetable;

    if (
      checkModAlreadyAdded(
        $currentlySelectedMods,
        acadYear,
        semester,
        timetable_id!,
        mod.moduleCode,
      )
    )
      return;
    currentlySelectedMods.set(
      await createModEntry(
        $currentlySelectedMods,
        acadYear,
        semester,
        timetable_id!,
        modFullInfo.moduleCode,
        timeTable,
        user_favourite_colour,
      ),
    );

    const new_data = get(currentlySelectedMods).find(
      (x) => x.id === timetable_id,
    )!.metaData;

    await $roomHub?.invoke("UpdateTimetable", timetable_id, {
      Name: timetable_name,
      MetaData: new_data,
    });

    search_term = "";
  }
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="flex h-12 w-full items-center gap-2 p-4" onclick={addMod}>
  {#if !mod.semesters.includes(semester)}
    <p class="font-semibold">{mod.moduleCode}</p>
    <p>{mod.title}</p>
    <p class="opacity-65">Not Offered in this semester</p>
  {:else}
    <p class="font-semibold">{mod.moduleCode}</p>
    <p>{mod.title}</p>
  {/if}
</div>
