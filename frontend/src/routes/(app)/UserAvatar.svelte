<script lang="ts">
  import { Edit, Pencil } from "@lucide/svelte";
  import { onMount } from "svelte";
  import Cropper from "svelte-easy-crop";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import UserAvatarComponent from "$lib/components/Profile/UserAvatarComponent.svelte";
  import {
    currentUserInformation,
    currentWorkingTimetable,
    token_information,
  } from "$lib/shared/shared.svelte";
  import GenericDialog from "./GenericDialog.svelte";
  import ProfilePictureChangeComponent from "./ProfilePictureChangeComponent.svelte";

  // svelte-ignore non_reactive_update
  let dialog: HTMLDialogElement;

  let { size = 40 } = $props();

  // svelte-ignore non_reactive_update
  let change_image_dialog: HTMLDialogElement;
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div onclick={() => dialog.show()}>
  <UserAvatarComponent {size}></UserAvatarComponent>
</div>

<GenericDialog
  bind:dialog
  closeHandler={() => {
    /* Intentionally Empty */
  }}
>
  <div class="flex flex-col items-center align-middle w-full">
    <div class="relative">
      <UserAvatarComponent size={128}></UserAvatarComponent>
      <div
        class="absolute bottom-0 right-1 w-10 h-10 bg-primary rounded-full flex items-center justify-center"
      >
        <Pencil
          class="w-4 h-4"
          onclick={() => {
            change_image_dialog.show();
          }}
        ></Pencil>
      </div>
    </div>

    <h3 class="text-2xl font-bold">
      Hi, {$currentUserInformation.username}
    </h3>
    <h3 class="italic">
      @{$currentUserInformation.handle}
    </h3>
    <div class="flex gap-1 w-full pt-2">
      <button
        class="btn btn-accent w-1/2"
        onclick={async () => {
          goto(resolve("/(app)/me"));
          dialog.close();
        }}>Manage Account</button
      >
      <button
        class="btn btn-error w-1/2"
        onclick={async () => {
          $token_information.a = "";
          $token_information.b = false;
          currentUserInformation.reset();
          currentWorkingTimetable.reset();
          const message = "Logout Successful";
          await goto(resolve(`/login?error_description=${message}`));
        }}>Logout</button
      >
    </div>
  </div>
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
