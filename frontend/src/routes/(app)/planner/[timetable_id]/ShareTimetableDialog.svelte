<script lang="ts">
  import UserAvatarComponent from "$lib/components/Profile/UserAvatarComponent.svelte";
  import { currentUserInformation } from "$lib/shared/shared.svelte";
  import type { RoomProfile, TimetableResponse } from "$lib/types/db_raw_types";
  import { format_room_role_to_string } from "$lib/utils/frontend_utils";
  import GenericDialog from "../../GenericDialog.svelte";

  let copy_text = $state("Copy Link!");
  interface ShareTimetableDialogProps {
    timetable_metadata: TimetableResponse;
    profiles: RoomProfile[];
    share_tt_dialog: HTMLDialogElement;
  }
  let {
    timetable_metadata,
    profiles,
    share_tt_dialog = $bindable(),
  }: ShareTimetableDialogProps = $props();
</script>

<GenericDialog
  bind:dialog={share_tt_dialog}
  closeHandler={() => (copy_text = "Copy Link!")}
>
  <h1 class=" text-xl font-bold">Share "{timetable_metadata.name}"</h1>
  <fieldset class="fieldset">
    <label class="label" for="name">Search for users by their handle:</label>
    <input type="text" id="name" class="input" placeholder="Name" />
    <p class="label">
      Handle can be found at the top right corner. Your handle is @{$currentUserInformation.handle}
    </p>
  </fieldset>
  <h1 class=" font-bold">People with Access:</h1>
  <ul class="list bg-base-100 rounded-box shadow-md">
    {#each profiles as profile}
      <li class="list-row">
        <div>
          <UserAvatarComponent user_info={profile}></UserAvatarComponent>
        </div>

        <div>
          <div>{profile.username}</div>
          <div class="text-xs font-semibold opacity-60">
            @{profile.handle}
          </div>
        </div>

        <div>
          <button class="btn">
            {format_room_role_to_string(profile.role)}
          </button>
        </div>
      </li>
    {/each}
  </ul>

  <button
    class="btn w-full btn-primary"
    onclick={async () => {
      copy_text = "Link Copied!";
      await navigator.clipboard.writeText(window.location.href);
    }}>{copy_text}</button
  >
</GenericDialog>
