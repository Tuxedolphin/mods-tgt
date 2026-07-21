<script lang="ts">
  import { Import } from "@lucide/svelte";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import { token_information } from "$lib/shared/shared.svelte";
  import {
    create_empty_timetable,
    put_timetable_by_id,
  } from "$lib/utils/db_operations";
  import {
    format_AY_name,
    format_semester_name,
  } from "$lib/utils/formatting_utils";
  import { parse_mods_link } from "$lib/utils/nusmods_parser";
  import GenericDialog from "../../routes/(app)/GenericDialog.svelte";

  // svelte-ignore non_reactive_update
  // oxlint-disable-next-line no-unassigned-vars
  let dialog: HTMLDialogElement;
  let timetable_name = $state("");
  let error = $state("");
  let sucess_text = $state("");
  let second_click = $state(false);
  let academic_year = $state("2026-2027");
  let share_link = $state("");

  async function create_new_empty_timetable() {
    error = "";
    const parsed_result = parse_mods_link(share_link);

    if (parsed_result.isErr()) {
      error = parsed_result.error;
    }

    if (second_click && parsed_result.isOk()) {
      const { mods_info, semester_no } = parsed_result.value;
      const timetable_info = await create_empty_timetable(
        $token_information.a,
        timetable_name,
        semester_no,
        academic_year,
      );

      if (timetable_info.isOk()) {
        const tt_info = timetable_info.value;
        const imported = await put_timetable_by_id(
          $token_information.a,
          tt_info.id,
          {
            academicYear: academic_year,
            createdAt: tt_info.createdAt,
            id: tt_info.id,
            name: tt_info.name,
            metaData: mods_info,
            semester: semester_no,
          },
        );

        if (imported.isOk()) {
          reset();
          dialog.close();
          goto(
            resolve("/(app)/planner/[timetable_id]", {
              timetable_id: timetable_info.value.id,
            }),
          );
        }
      }
    }

    if (parsed_result.isOk()) {
      second_click = true;

      sucess_text = `Managed to detect ${parsed_result.value.no_mods_detected} mods for ${format_semester_name(parsed_result.value.semester_no)} (${format_AY_name(academic_year)})`;
    }

    // if (timetable_info.isOk()) {
    // 	dialog.close();
    // 	goto(resolve('/(app)/planner/[timetable_id]', { timetable_id: timetable_info.value.id }));
    // }
  }

  function reset() {
    error = sucess_text = "";
    second_click = false;
  }
</script>

<Import size={32} class="cursor-pointer" onclick={() => dialog.show()}></Import>
<!-- Open the modal using ID.showModal() method -->

<GenericDialog
  bind:dialog
  closeHandler={() => {
    reset();
  }}
>
  <h3 class="text-lg font-bold">Import from NUSMods! (Beta)</h3>

  <h3 class="text-error">
    Importing from NUSMods Share Link is currently work in progress, please
    check any errors after the import is complete!
  </h3>
  <p class="py-4">Name your timetable:</p>
  <input class="input" bind:value={timetable_name} />

  <p class="py-4">Choose AY (Make sure your AY Matches the one in NUSMods!)</p>
  <select class="select" bind:value={academic_year}>
    <option>2026-2027</option>
    <option>2025-2026</option>
    <option>2024-2025</option>
  </select>

  <p class="py-4">Paste NUS Mods Share Link:</p>
  <input class="input" bind:value={share_link} />
  {#if error}
    <div class="text-error">
      <p>Error reading share link:</p>
      <p>Problem: {error}</p>
      <p>
        Do also note that this feature is currently work in progress. It may
        have issues reading certain NUSMods Share links.
      </p>
    </div>
  {/if}

  {#if sucess_text}
    <div class="text-success">
      <p>Share link is valid!</p>
      <p>{sucess_text}</p>
      <p>Click on Create timetable again to confirm!</p>
    </div>
  {/if}

  <div class="modal-action">
    <!-- if there is a button in form, it will close the modal -->
    <button class="btn btn-primary" onclick={() => create_new_empty_timetable()}
      >Create timetable</button
    >
    <button
      class="btn btn-error"
      onclick={() => {
        reset();

        dialog.close();
      }}>Cancel</button
    >
  </div>
</GenericDialog>
