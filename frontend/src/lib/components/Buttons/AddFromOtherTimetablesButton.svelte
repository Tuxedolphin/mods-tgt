<script lang="ts">
  import { token_information } from "$lib/shared/shared.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import type { TimetableInfos } from "$lib/types/db_raw_types";
  import { get_timetables } from "$lib/utils/db_operations";
  import {
    format_AY_name,
    format_semester_name,
  } from "$lib/utils/formatting_utils";
  import GenericDialog from "../../../routes/(app)/GenericDialog.svelte";

  interface AddFromOtherTimetablesButtonProps {
    acad_year: string;
    semester: number;
    current_timetable_id: string;
  }
  let dialog: HTMLDialogElement;
  let timetable_infos: TimetableInfos | undefined = $state();
  let selected_tt_id = $state("");
  let {
    acad_year,
    semester,
    current_timetable_id,
  }: AddFromOtherTimetablesButtonProps = $props();
</script>

<button
  class="btn btn-primary"
  onclick={async () => {
    const available_timetables_req = await get_timetables($token_information.a);

    if (available_timetables_req.isOk()) {
      const available_timetable = available_timetables_req.value;
      timetable_infos = available_timetable;

      timetable_infos = timetable_infos.filter(
        (x) => x.academicYear === acad_year && x.semester === semester,
      );

      if (timetable_infos.length !== 0) {
        selected_tt_id = timetable_infos[0].id;
      }
    }

    if (available_timetables_req.isErr()) {
      timetable_infos = undefined;
    }
    dialog.show();
  }}>Copy from other timetables</button
>

<GenericDialog
  bind:dialog
  closeHandler={() => {
    /* Intentionally Left Empty */
  }}
>
  <h3 class="text-lg font-bold">Copy from your other timetables!</h3>
  {#if timetable_infos}
    {#if timetable_infos.length !== 0}
      <p class="text">Select the timetable:</p>
      <div class="w-full flex gap-1">
        <select class="select w-full" bind:value={selected_tt_id}>
          {#each timetable_infos as info}
            <option value={info.id}
              >{info.name} - {new Date(
                info.createdAt,
              ).toLocaleDateString()}</option
            >
          {/each}
        </select>
        <button
          class="btn btn-primary"
          onclick={async () => {
            // console.log(current_timetable_id)
            await $roomHub
              ?.invoke(
                "CopyTimetableTo",
                selected_tt_id,
                current_timetable_id,
              )
              .catch((e) => console.error(e));
          }}>Copy!</button
        >
      </div>
    {:else}
      <p class="text-error">
        You do not have any other timetables created in the same semester and
        academic year.
      </p>
    {/if}
  {:else}
    <p class="text-error">Error loading timetables.</p>
  {/if}
</GenericDialog>
