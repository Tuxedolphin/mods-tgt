<script lang="ts">
  import { onMount } from "svelte";

  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import {
    currentUserInformation,
    currentWorkingTimetable,
    token_information,
  } from "$lib/shared/shared.svelte";
  import LoginButton from "./LoginButton.svelte";

  let emailInput = $state("");
  let passwordInput = $state("");
  let errorMessage = $state("");
  onMount(() => {
    for (const [key, value] of new URLSearchParams(window.location.hash)) {
      if (key.includes("access_token")) {
        $token_information.a = value;
        goto(resolve("/home"));
      }

      if (key.includes("error_description")) {
        errorMessage = value;
      }
    }

    for (const [key, value] of new URLSearchParams(window.location.search)) {
      if (key.includes("error_description")) {
        errorMessage = value;
      }

      if (key.includes("action")) {
        if (value === "flush") {
          $token_information.a = "";
          $token_information.b = false;
          currentUserInformation.reset();
          currentWorkingTimetable.reset();
        }
      }
    }
  });
</script>

<div class="text-error">{errorMessage}</div>
<fieldset
  class="fieldset bg-base-200 border-base-300 rounded-box w-xs border p-4"
>
  <legend class="fieldset-legend">Login</legend>

  <!-- svelte-ignore a11y_label_has_associated_control -->
  <label class="label">Email</label>
  <input
    type="email"
    bind:value={emailInput}
    class="input validator"
    placeholder="Email"
  />

  <!-- svelte-ignore a11y_label_has_associated_control -->
  <label class="label">Password</label>
  <input
    type="password"
    bind:value={passwordInput}
    class="input"
    placeholder="Password"
  />
  <LoginButton email={emailInput} password={passwordInput} bind:errorMessage
  ></LoginButton>
</fieldset>
<div>
  <button
    class="cursor-pointer underline"
    onclick={() => goto(resolve("/forgot-password"))}>Forgot Password?</button
  >
</div>

<div>
  Don't have an account yet? Register <button
    class="cursor-pointer underline"
    onclick={() => goto(resolve("/register"))}>here</button
  >
</div>
