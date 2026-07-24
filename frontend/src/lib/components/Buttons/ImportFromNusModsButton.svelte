<script lang="ts">
  import {
    currentUserInformation,
    token_information,
  } from "$lib/shared/shared.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import type {
    TimetableInfos,
    TimetablePostTemplate,
  } from "$lib/types/db_raw_types";
  import { get_timetables } from "$lib/utils/db_operations";
  import {
    format_AY_name,
    format_semester_name,
  } from "$lib/utils/formatting_utils";
  import { parse_mods_link } from "$lib/utils/nusmods_parser";
  import GenericDialog from "../../../routes/(app)/GenericDialog.svelte";

  interface AddFromOtherTimetablesButtonProps {
    acad_year: string;
    semester: number;
    current_timetable_id: string | undefined;
    timetable_name: string;
    user_favourite_colour: string;
  }

  // svelte-ignore non_reactive_update
  let dialog: HTMLDialogElement;
  let share_link = $state("");
  let error = $state("");
  let sucess_text = $state("");
  let second_click = $state(false);

  let {
    acad_year,
    current_timetable_id,
    timetable_name,
    semester,
    user_favourite_colour,
  }: AddFromOtherTimetablesButtonProps = $props();

  function reset() {
    error = sucess_text = share_link = "";
    second_click = false;
  }
</script>

<button
  class="btn btn-primary"
  onclick={async () => {
    reset();
    dialog.show();
  }}>Import from NUSMods Link (Beta)</button
>

<GenericDialog
  bind:dialog
  closeHandler={() => {
    reset();
  }}
>
  <div class="text-left">
    <h3 class="text-lg font-bold">Import from NUSMods! (Beta)</h3>

    <h3 class="text-error">
      Importing from NUSMods Share Link is currently work in progress, please
      check any errors after the import is complete!
    </h3>

    <p class="py-4">Paste NUS Mods Share Link:</p>
    <div class="flex w-full gap-1">
      <input class="input w-full" bind:value={share_link} />
      <button
        class="btn btn-primary"
        onclick={async () => {
          const parsed_result = parse_mods_link(
            share_link,
            user_favourite_colour,
          );

          if (second_click && parsed_result.isOk()) {
            const { mods_info } = parsed_result.value;

            if (!current_timetable_id) {
              const info_to_post: TimetablePostTemplate = {
                academicYear: acad_year,
                metaData: [],
                name: timetable_name,
                semester: semester,
              };
              await $roomHub?.invoke("CreateTimetable", info_to_post);
            }

            await $roomHub?.invoke("UpdateTimetable", current_timetable_id, {
              Name: timetable_name,
              MetaData: mods_info,
            });
            reset();
          }

          if (parsed_result.isOk()) {
            second_click = true;

            sucess_text = `Managed to detect ${parsed_result.value.no_mods_detected} mods for ${format_semester_name(parsed_result.value.semester_no)} (${format_AY_name(acad_year)})`;
          }

          if (parsed_result.isErr()) {
            error = parsed_result.error;
          }
        }}>Import</button
      >
    </div>

    {#if sucess_text}
      <div class="text-success">
        <p>Share link is valid!</p>
        <p>{sucess_text}</p>
        <p>Click on 'Import' again to confirm!</p>
      </div>
    {/if}
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
  </div>
</GenericDialog>
