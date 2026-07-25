<script lang="ts">
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import {
    token_information,
    timetable_list_should_be_refreshed,
    currentUserInformation,
  } from "$lib/shared/shared.svelte";
  import {
    delete_timetable_by_id,
    put_timetable_by_id,
    rename_tt_by_id,
  } from "$lib/utils/db_operations";
  import {
    format_AY_name,
    format_semester_name,
  } from "$lib/utils/formatting_utils";
  import { Pencil, Trash2, SquareArrowOutUpRight } from "@lucide/svelte";
  import GenericDialog from "../GenericDialog.svelte";
  import type {
    TimetableSummaryResponse,
    SharedTimetableSummaryResponse,
  } from "$lib/types/db_raw_types";

  let selected_timetable_name = $state("");
  let selected_timetable_id = $state("");
  let new_tt_name = $state("");
  let edit_error = $state("");
  let deletion_dialog: HTMLDialogElement | undefined = $state();
  let editing_dialog: HTMLDialogElement | undefined = $state();
  interface TimetableListProps {
    editing_allowed: boolean;
    deletion_allowed: boolean;
    availableTimetables: SharedTimetableSummaryResponse[];
  }

  let {
    availableTimetables,
    editing_allowed = true,
    deletion_allowed = true,
  }: TimetableListProps = $props();
</script>

<div class="overflow-x-auto">
  <table class="table">
    <!-- head -->
    <thead>
      <tr>
        <th>Name</th>
        <th>Academic Year</th>
        <th>Semester</th>
        <th class="hidden md:block">Created</th>
        <th>Owner</th>
        <th>Actions</th>
      </tr>
    </thead>
    <tbody>
      <!-- row 1 -->
      {#each availableTimetables as timetable, i}
        {@const date = new Date(timetable.createdAt)}
        <tr
          class="hover:bg-base-300 cursor-pointer"
          onclick={(e) => {
            e.preventDefault();
            goto(
              resolve("/(app)/planner/[timetable_id]", {
                timetable_id: timetable.id,
              }),
            );
          }}
        >
          <td>{timetable.name}</td>

          <td>{format_AY_name(timetable.academicYear)}</td>
          <td>{format_semester_name(timetable.semester)}</td>
          <td class="hidden md:block"
            >{date.toDateString()}, {date.toLocaleTimeString()}</td
          >
          <td
            >{#if timetable.profile.handle === $currentUserInformation.handle}
              You
            {:else}
              @{timetable.profile.handle}
            {/if}</td
          >
          <td>
            <div class="flex gap-2">
              {#if editing_allowed}
                <Pencil
                  size={28}
                  onclick={(e) => {
                    selected_timetable_name = timetable.name;
                    selected_timetable_id = timetable.id;
                    new_tt_name = timetable.name;
                    e.stopPropagation();
                    editing_dialog?.show();
                  }}
                ></Pencil>
              {/if}

              {#if deletion_allowed}
                <Trash2
                  size={28}
                  onclick={(e) => {
                    selected_timetable_name = timetable.name;
                    selected_timetable_id = timetable.id;
                    e.stopPropagation();
                    deletion_dialog?.show();
                  }}
                ></Trash2>
              {/if}

              <SquareArrowOutUpRight
                size={28}
                onclick={() =>
                  goto(
                    resolve("/(app)/planner/[timetable_id]", {
                      timetable_id: timetable.id,
                    }),
                  )}
              />
            </div>
          </td>
        </tr>
      {/each}
    </tbody>
  </table>
</div>

<!-- Deletion -->
<GenericDialog
  bind:dialog={deletion_dialog}
  closeHandler={() => {
    /* Intentionally Left Empty */
  }}
>
  <h3 class="text-lg font-bold">Confirm?</h3>
  <p class="py-4">
    Delete the timetable: '{selected_timetable_name}' (this action is
    irreversible!)
  </p>

  <div class="modal-action">
    <button
      class="btn btn-primary"
      onclick={async () => {
        await delete_timetable_by_id(
          $token_information.a,
          selected_timetable_id,
        );
        timetable_list_should_be_refreshed.set(true);
        deletion_dialog?.close();
      }}>Delete!</button
    >
    <button class="btn btn-error" onclick={() => deletion_dialog?.close()}
      >Cancel</button
    >
  </div>
</GenericDialog>

<!-- Editing -->
<GenericDialog
  bind:dialog={editing_dialog}
  closeHandler={() => {
    /* Intentionally Left Empty */
  }}
>
  <h3 class="text-lg font-bold">Confirm?</h3>
  <p class="py-4">
    Rename your timetable '{selected_timetable_name}'
  </p>
  <input class="input" bind:value={new_tt_name} />
  <p class="text-error">{edit_error}</p>
  <div class="modal-action">
    <button
      class="btn btn-primary"
      onclick={async () => {
        edit_error = "";
        const edit = await rename_tt_by_id(
          $token_information.a,
          selected_timetable_id,
          new_tt_name,
        );
        timetable_list_should_be_refreshed.set(true);

        if (edit.isErr()) {
          edit_error = "Error renaming. Please try again.";
          return;
        }
        editing_dialog?.close();
      }}>Rename!</button
    >
    <button class="btn btn-error" onclick={() => editing_dialog?.close()}
      >Cancel</button
    >
  </div>
</GenericDialog>
