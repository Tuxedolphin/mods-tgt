<script lang="ts">
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import ModTogetherHero from "$lib/components/LoginPage/ModTogetherHero.svelte";
  import {
    currentUserInformation,
    token_information,
  } from "$lib/shared/shared.svelte";
  import type { Profile } from "$lib/types/db_raw_types";
  import {
    get_user_info,
    put_user_info,
    update_user_preferences,
    update_user_profile,
  } from "$lib/utils/db_operations";
  import { colours } from "$lib/utils/formatting_utils";
  import { query_available_handle } from "$lib/utils/frontend_utils";
  import { Check } from "@lucide/svelte";
  import { onMount } from "svelte";
  let welcome_back = $state(false);
  let loading = $state(false);

  let current_user_info = $state({} as Profile);

  onMount(async () => {
    const urlParams = new URLSearchParams(window.location.search);

    if (urlParams.get("action") === "back") {
      welcome_back = true;
    }

    const user_info = await get_user_info($token_information.a, false);

    if (user_info.isOk()) {
      current_user_info = user_info.value;

      if (!current_user_info.colour) {
        current_user_info.colour = "bg-emerald-400";
      }

      if (!current_user_info.defaultTheme) {
        current_user_info.defaultTheme = "system";
      }
    }

    await check_handle_safe();
  });

  async function update_account() {
    handle_error = change_error = "";
    loading = true;
    const change = await update_user_profile(
      current_user_info,
      $token_information.a,
    );

    const change_preferences = await update_user_preferences(
      current_user_info,
      $token_information.a,
    );

    if (!change_preferences.isOk()) {
      console.log(change_preferences.error);
      change_error = change_preferences.error;
      loading = false;
      return;
    }

    if (!change.isOk()) {
      change_error = change.error;
      loading = false;
      return;
    }

    $currentUserInformation = current_user_info;

    goto(resolve("/(app)/home"));

    loading = false;
  }

  async function check_handle_safe() {
    check_handle_state = handle_success = handle_error = "";
    check_handle_state = "Checking Handle availability...";
    query_available_handle(
      current_user_info.handle!,
      $token_information.a,
      (fail) => {
        check_handle_state = handle_success = handle_error = "";
        handle_error = fail;
      },
      (success) => {
        check_handle_state = handle_success = handle_error = "";
        handle_success = success;
      },
    );
  }

  let handle_error = $state("");
  let handle_success = $state("");
  let check_handle_state = $state("");
  let change_error = $state("");
</script>

<ModTogetherHero attempt_redirect={false}>
  {#if welcome_back}
    <h1 class="font-semibold">
      Mods Together was updated recently, we will need some new information from
      you!
    </h1>
  {:else}
    <h1 class="font-semibold">
      Welcome to Mods Together, tell us more about you!
    </h1>
  {/if}

  <fieldset class="fieldset">
    <legend class="fieldset-legend">Your name:</legend>
    <input type="text" class="input" bind:value={current_user_info.username} />
    {#if change_error}
      <p class="text-error">{change_error}</p>
    {/if}
  </fieldset>

  <fieldset class="fieldset">
    <legend class="fieldset-legend">Your Handle</legend>

    <input
      type="text"
      class="input"
      bind:value={current_user_info.handle}
      oninput={async () => {
        check_handle_safe();
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
    <legend class="fieldset-legend">What's your favorite colour?</legend>
    <div class="grid grid-cols-8 gap-1">
      {#each colours as colour (colour)}
        <!-- svelte-ignore a11y_consider_explicit_label -->
        <button
          onclick={() => {
            current_user_info.colour = colour;
          }}
          class="flex items-center justify-around {colour} w-8 h-8 rounded-4xl text-black"
        >
          {#if current_user_info.colour === colour}
            <Check></Check>
          {/if}
        </button>
      {/each}
    </div>
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
</ModTogetherHero>
