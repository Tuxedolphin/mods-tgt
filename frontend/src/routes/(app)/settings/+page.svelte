<script lang="ts">
  import { onDestroy, onMount } from "svelte";
  import type { PageProps } from "./$types";
  import {
    get_user_info,
    update_user_preferences,
    update_user_profile,
  } from "$lib/utils/db_operations";
  import {
    currentUserInformation,
    is_logging_out,
    token_information,
  } from "$lib/shared/shared.svelte";
  import {
    available_theme_preferences,
    type Profile,
  } from "$lib/types/db_raw_types";
  import { setTheme } from "mode-watcher";
  import { setMode } from "mode-watcher";
  import { modeStorageKey } from "mode-watcher";
  import {
    daisy_ui_themes,
    format_theme_preference_to_string,
  } from "$lib/utils/frontend_utils";

  let current_user_info: Profile | undefined = $state();
  let previous_user_info: Profile | undefined = $state();
  let loading = $state(false);
  let current_themes = $derived(daisy_ui_themes.toSorted());
  onMount(async () => {
    const user_info = await get_user_info($token_information.a, false);

    if (user_info.isOk()) {
      current_user_info = user_info.value;
      previous_user_info = user_info.value;
    }
  });

  async function update_account() {
    loading = true;

    const change_preferences = await update_user_preferences(
      current_user_info!,
      $token_information.a,
    );

    if (!change_preferences.isOk()) {
      //handle_error = change_preferences.error;
      loading = false;
      return;
    }

    previous_user_info = current_user_info;

    // window.location.reload();
    loading = false;
  }

  onDestroy(() => {
    $currentUserInformation.defaultTheme = previous_user_info?.defaultTheme;
  });
</script>

<p class="mt-2 text-xl font-semibold">Settings</p>
<hr class="my-2 h-px border-0 bg-gray-200" />

{#if current_user_info}
  <fieldset class="fieldset">
    <legend class="fieldset-legend">Your theme preference:</legend>
    <select
      class="select capitalize"
      bind:value={current_user_info.defaultTheme}
      onchange={() => {
        setTheme(current_user_info!.defaultTheme!);
      }}
    >
      {#each current_themes as theme_preference}
        <option value={theme_preference}
          ><p class="capitalize">{theme_preference}</p></option
        >
      {/each}
    </select>
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
{/if}
