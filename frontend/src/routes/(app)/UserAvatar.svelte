<script lang="ts">
  import ProfilePictureChangeComponent from "./ProfilePictureChangeComponent.svelte";

  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import {
    currentUserInformation,
    currentWorkingTimetable,
    token_information,
  } from "$lib/shared/shared.svelte";
  import Cropper from "svelte-easy-crop";
  import GenericDialog from "./GenericDialog.svelte";
  import { onMount } from "svelte";

  // svelte-ignore non_reactive_update
  let dialog: HTMLDialogElement;

  let change_image_dialog: HTMLDialogElement;
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div class="avatar avatar-placeholder" onclick={() => dialog.show()}>
  <div class="w-12 rounded-full bg-neutral text-neutral-content">
    {#if $currentUserInformation.avatarUrl}
      <img src={$currentUserInformation.avatarUrl} />
    {:else}
      <span class="text-3xl">{$currentUserInformation.username?.charAt(0)}</span
      >
    {/if}
  </div>
</div>

<GenericDialog
  bind:dialog
  closeHandler={() => {
    /* Intentionally Empty */
  }}
>
  <h3 class="text-lg font-bold">Hi, {$currentUserInformation.username}</h3>

  <button
    class="btn btn-error"
    onclick={async () => {
      $token_information.a = "";
      $token_information.b = false;
      currentUserInformation.reset();
      currentWorkingTimetable.reset();
      const message = "Logout Successful";

      // setTimeout(() => {}, 1000);

      await goto(resolve(`/login?error_description=${message}`));
    }}>Logout</button
  >

  <button
    class="btn btn-accent"
    onclick={() => {
      change_image_dialog.show();
    }}>Change Pfp</button
  >
</GenericDialog>

<GenericDialog
  bind:dialog={change_image_dialog}
  closeHandler={() => {
    /*Intentially Left Empty*/
  }}
>
  <ProfilePictureChangeComponent parentDialog={change_image_dialog}
  ></ProfilePictureChangeComponent>
</GenericDialog>
