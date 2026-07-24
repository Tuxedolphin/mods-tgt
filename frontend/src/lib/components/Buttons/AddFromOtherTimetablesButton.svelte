<script lang="ts">
  import { token_information } from "$lib/shared/shared.svelte";
  import { roomHub } from "$lib/stores/roomHub";
  import type {
    TimetableInfos,
    TimetablePostTemplate,
  } from "$lib/types/db_raw_types";
  import { get_timetables } from "$lib/utils/db_operations";
  import GenericDialog from "../../../routes/(app)/GenericDialog.svelte";

  interface AddFromOtherTimetablesButtonProps {
    acad_year: string;
    semester: number;
    current_timetable_id: string | undefined;
    timetable_name: string;
  }
  // svelte-ignore non_reactive_update
  let dialog: HTMLDialogElement;
  let timetable_infos: TimetableInfos | undefined = $state();
  let selected_tt_id = $state("");
  let {
    acad_year,
    semester,
    current_timetable_id,
    timetable_name,
  }: AddFromOtherTimetablesButtonProps = $props();
</script>

<button
  class="btn btn-primary w-full"
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
  }}>Copy From Existing Timetable</button
>

<GenericDialog
  bind:dialog
  closeHandler={() => {
    /* Intentionally Left Empty */
  }}
>
  <h3 class="text-lg font-bold">Copy from existing timetables! (Beta)</h3>
  <p class="text-error">
    Copying from timetables is a feature that is work in progress. It currently
    does not work with certain timetable configurations (like empty timetables),
    and sometimes requires refreshing the page and copying again for it to work.
  </p>
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
            if (!current_timetable_id) {
              const info_to_post: TimetablePostTemplate = {
                academicYear: acad_year,
                metaData: [],
                name: timetable_name,
                semester: semester,
              };
              await $roomHub?.invoke("CreateTimetable", info_to_post);
            }

            await $roomHub?.invoke(
              "CopyTimetableTo",
              selected_tt_id,
              current_timetable_id,
            );
            if (dialog) {
              dialog.close();
            }
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
