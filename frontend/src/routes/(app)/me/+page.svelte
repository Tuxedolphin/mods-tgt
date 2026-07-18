<script lang="ts">
  import GreetingComponent from "$lib/components/GreetingComponent.svelte";
  import UserAvatarComponent from "$lib/components/Profile/UserAvatarComponent.svelte";
  import { onMount } from "svelte";
  import type { PageProps } from "./$types";
  import { delete_user, get_user_info } from "$lib/utils/db_operations";
  import {
    currentUserInformation,
    token_information,
  } from "$lib/shared/shared.svelte";
  import type { Profile } from "$lib/types/db_raw_types";
  import { redirect } from "@sveltejs/kit";

  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import { get } from "svelte/store";

  let { data }: PageProps = $props();
  let current_user_info = $state({} as Profile);
  let current_password = $state("");
  let new_password = $state("");
  let loading = $state(false);
  let confirm_delete = $state("");

  let delete_error = $state("");
  onMount(async () => {
    const user_info = await get_user_info($token_information.a, false);

    if (user_info.isOk()) {
      current_user_info = user_info.value;
    }
  });

  async function delete_account() {
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

<div class="container mx-auto">
  <p class="mt-2 text-xl font-semibold">Customise your profile!</p>
  <hr class="my-2 h-px border-0 bg-gray-200" />

  <fieldset class="fieldset">
    <legend class="fieldset-legend">Profile Picture</legend>
    <UserAvatarComponent size={128}></UserAvatarComponent>
  </fieldset>

  <fieldset class="fieldset">
    <legend class="fieldset-legend">Your name:</legend>
    <input type="text" class="input" bind:value={current_user_info.username} />
  </fieldset>

  <fieldset class="fieldset">
    <legend class="fieldset-legend">Your Handle</legend>
    <input type="text" class="input" bind:value={current_user_info.handle} />
  </fieldset>
  <fieldset class="fieldset">
    <button class="btn btn-primary max-w-xs {loading ? 'btn-disabled' : ''}"
      >Save Changes</button
    >
  </fieldset>

  <p class="mt-2 text-xl font-semibold">Account Security</p>
  <hr class="my-2 h-px border-0 bg-gray-200" />

  <fieldset class="fieldset">
    <legend class="fieldset-legend">Current password</legend>
    <input type="text" class="input" bind:value={current_password} />

    <legend class="fieldset-legend">New password</legend>
    <input type="text" class="input" bind:value={new_password} />

    <button class="btn btn-primary max-w-xs {loading ? 'btn-disabled' : ''}"
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
</div>
