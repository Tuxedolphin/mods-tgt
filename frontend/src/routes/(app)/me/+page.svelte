<script lang="ts">
  import { Pencil } from "@lucide/svelte";
  import { debounce } from "es-toolkit/function";
  import { onMount } from "svelte";
  import { get } from "svelte/store";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import UserAvatarComponent from "$lib/components/Profile/UserAvatarComponent.svelte";
  import {
    currentUserInformation,
    token_information,
  } from "$lib/shared/shared.svelte";
  import type { Profile } from "$lib/types/db_raw_types";
  import {
    check_handle,
    delete_user,
    get_user_info,
    update_user_password,
    update_user_profile,
  } from "$lib/utils/db_operations";
  import GenericDialog from "../GenericDialog.svelte";
  import ProfilePictureChangeComponent from "../ProfilePictureChangeComponent.svelte";
  import type { PageProps } from "./$types";

  let current_user_info = $state({} as Profile);

  let loading = $state(false);
  let confirm_delete = $state("");
  let change_image_dialog: HTMLDialogElement;

  let delete_error = $state("");

  onMount(async () => {
    const user_info = await get_user_info($token_information.a, false);

    if (user_info.isOk()) {
      current_user_info = user_info.value;
    }
  });

  // Handle:
  let handle_error = $state("");
  let handle_success = $state("");
  let check_handle_state = $state("");
  const query_available_handle = debounce(async (handle: string) => {
    const result = await check_handle(handle, $token_information.a);

    if (result.isOk()) {
      check_handle_state = handle_success = handle_error = "";

      if (!result.value.available) {
        switch (result.value.reason) {
          case "invalidFormat":
            handle_error = "Handle is in an invalid format, please try again";
            break;
          case "reserved":
            handle_error =
              "Handle has already been reserved. Please try another";
            break;
          case "taken":
            handle_error = "Handle is taken, please try another.";
            break;
          case "tooLong":
            handle_error = "Handle is too long, please try another.";
            break;
          case "tooShort":
            handle_error =
              "Handle is too short, must be at least 4 characters.";
            break;
          default:
            break;
        }

        return;
      }

      handle_success = "Handle is available!";
      return;
    }

    check_handle_state = handle_success = handle_error = "";
    handle_error = result.error;
  }, 500);

  let password_error = $state("");
  let password_success = $state("");
  let old_password = $state("");
  let new_password = $state("");
  async function update_password() {
    password_error = password_success = "";
    loading = true;
    const result = await update_user_password(
      old_password,
      new_password,
      $token_information.a,
    );

    if (result.isOk()) {
      password_success = "Password updated!";
      old_password = new_password = "";
    } else {
      password_error = result.error;
    }
    loading = false;
  }

  async function update_account() {
    handle_error = "";
    loading = true;
    const change = await update_user_profile(
      current_user_info,
      $token_information.a,
    );

    if (change.isOk()) {
      window.location.reload();
    } else {
      handle_error = change.error;
    }
    loading = false;
  }

  async function delete_account() {
    handle_error = "";
    loading = true;
    const result = await delete_user($token_information.a);

    if (result.isOk()) {
      const username = get(currentUserInformation).username;
      goto(resolve(`/forget-me-not?user=${username}`));
    } else {
      delete_error = result.error;
    }
    loading = false;
  }
</script>

<p class="mt-2 text-xl font-semibold">Customise your profile!</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

<fieldset class="fieldset flex">
  <div class="relative">
    <UserAvatarComponent size={128}></UserAvatarComponent>
    <!-- svelte-ignore a11y_click_events_have_key_events -->
    <!-- svelte-ignore a11y_no_static_element_interactions -->
    <div
      class="absolute bottom-0 right-1 w-10 h-10 bg-primary rounded-full flex items-center justify-center"
      onclick={() => {
        change_image_dialog.show();
      }}
    >
      <Pencil class="w-4 h-4"></Pencil>
    </div>
  </div>
</fieldset>

<fieldset class="fieldset">
  <legend class="fieldset-legend">Your name:</legend>
  <input type="text" class="input" bind:value={current_user_info.username} />
</fieldset>

<fieldset class="fieldset">
  <legend class="fieldset-legend">Your Handle</legend>

  <input
    type="text"
    class="input"
    bind:value={current_user_info.handle}
    oninput={async (new_text) => {
      check_handle_state = handle_success = handle_error = "";
      check_handle_state = "Checking Handle availability...";
      query_available_handle(current_user_info.handle!);
    }}
  />
  {#if handle_error}
    <p class="text-error">{handle_error}</p>
  {/if}

  {#if check_handle_state}
    <p class="italic">{check_handle_state}</p>
  {/if}

  {#if handle_success}
    <p class="text-success">{handle_success}</p>
  {/if}
</fieldset>
<fieldset class="fieldset">
  <button
    onclick={async () => {
      await update_account();
    }}
    class="btn btn-primary max-w-xs {loading ? 'btn-disabled' : ''}"
    >Save Changes</button
  >
</fieldset>

<p class="mt-2 text-xl font-semibold">Account Security</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

<fieldset class="fieldset">
  <legend class="fieldset-legend">Current password</legend>
  <input type="password" class="input" bind:value={old_password} />

  <legend class="fieldset-legend">New password</legend>
  <input type="password" class="input" bind:value={new_password} />

  {#if password_error}
    <p class="text-error">{password_error}</p>
  {/if}

  {#if password_success}
    <p class="text-success">{password_success}</p>
  {/if}
  <button
    onclick={async () => {
      await update_password();
    }}
    class="btn btn-primary max-w-xs {loading ? 'btn-disabled' : ''}"
    >Change Password</button
  >
</fieldset>

<p class="mt-2 text-xl font-semibold text-error">DANGER ZONE!</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

<fieldset class="fieldset">
  <legend class="fieldset-legend">Confirm your handle:</legend>
  <input
    type="text"
    class="input"
    placeholder="Type your handle"
    bind:value={confirm_delete}
  />
  <p class="text-error">{delete_error}</p>
  <p class="label">
    We're sorry to see you go. To delete your account, type your handle ({$currentUserInformation.handle})
    and press the delete button
  </p>
</fieldset>
<fieldset class="fieldset">
  <button
    onclick={async () => {
      await delete_account();
    }}
    class="btn btn-error max-w-xs {loading
      ? 'btn-disabled'
      : ''} {confirm_delete !== $currentUserInformation.handle
      ? 'btn-disabled'
      : ''}">Delete my account</button
  >
</fieldset>

<GenericDialog
  bind:dialog={change_image_dialog}
  closeHandler={() => {
    /*Intentially Left Empty*/
  }}
>
  <ProfilePictureChangeComponent parentDialog={change_image_dialog}
  ></ProfilePictureChangeComponent>
</GenericDialog>
