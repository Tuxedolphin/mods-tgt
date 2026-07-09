<script lang="ts">
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import { registered } from "$lib/shared/shared.svelte";
  import { register_db } from "$lib/utils/db_operations";
  import RegisterButton from "./RegisterButton.svelte";

  let emailInput = $state("");
  let passwordInput = $state("");
  let confirmPasswordInput = $state("");
  let loading = $state(false);
  let error = $state("");
  let success = $state("");
</script>

{#if success}
  <p class="text-success text-wrap break-after-all">{success}</p>
{:else}
  <fieldset
    class="fieldset bg-base-200 border-base-300 rounded-box w-xs border p-4"
  >
    <p class="text-error text-wrap break-after-all">
      {error}
    </p>

    <legend class="fieldset-legend">Register</legend>

    <label class="label">Email</label>
    <input
      type="email"
      bind:value={emailInput}
      class="input validator"
      placeholder="Email"
    />

    <label class="label">Password</label>
    <input
      type="password"
      bind:value={passwordInput}
      class="input"
      placeholder="Confirm Password"
    />

    <label class="label">Confirm Password</label>
    <input
      type="password"
      bind:value={confirmPasswordInput}
      class="input"
      placeholder="Confirm Password"
    />

    <button
      class="btn btn-primary mt-4 {loading ? 'btn-disabled' : ''}"
      onclick={async () => {
        loading = true;
        error = success = "";
        if (passwordInput !== confirmPasswordInput) {
          error = "Passwords should match!";
          loading = false;
          return;
        }

        if (passwordInput.length < 6) {
          error = "Passwords should be 6 characters or longer";
          loading = false;
          return;
        }
        const result = await register_db(emailInput, passwordInput);

        if (result.isErr()) {
          error = result.error;
        } else {
          success =
            "Registration successful. A confirmation will be sent if email has not been used before. Check spam if necessary.";
        }
        loading = false;
      }}
    >
      Register!</button
    >

    <button
      class="btn btn-secondary"
      onclick={() => {
        goto(resolve("/login"));
      }}>Back to login</button
    >
  </fieldset>
{/if}
