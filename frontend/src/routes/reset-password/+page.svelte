<script lang="ts">
  import { onMount } from "svelte";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import ModTogetherHero from "$lib/components/LoginPage/ModTogetherHero.svelte";
  import { reset_password } from "$lib/utils/db_operations";
  import { sleep } from "$lib/utils/frontend_utils";

  let token_hash = $state("");
  let type = $state("");
  let passwordInput = $state("");
  let confirmPasswordInput = $state("");
  let loading = $state(false);
  let error = $state("");
  let success = $state("");
  onMount(() => {
    const search_params = new URLSearchParams(window.location.search);

    for (const [key, value] of search_params) {
      if (key === "token_hash") {
        token_hash = value;
      }

      if (key === "type") {
        type = value;
      }
    }

    if (token_hash === "" || type === "") {
      goto(resolve("/"));
    }
  });
</script>

<ModTogetherHero>
  <p class="text-error text-wrap break-all">
    {error}
  </p>
  <p class="text-success text-wrap break-all">{success}</p>
  <fieldset
    class="fieldset bg-base-200 border-base-300 rounded-box w-xs border p-4"
  >
    <legend class="fieldset-legend">Reset Password!</legend>

    <label class="label">Password</label>
    <input
      type="password"
      bind:value={passwordInput}
      class="input"
      placeholder="Password"
    />

    <label class="label">Confirm Password</label>
    <input
      type="password"
      bind:value={confirmPasswordInput}
      class="input"
      placeholder="Confirm Password"
    />

    <button
      class="btn btn-neutral mt-4 {loading ? 'btn-disabled' : ''}"
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
        const result = await reset_password(token_hash, passwordInput);

        if (result.isErr()) {
          error = result.error;
        } else {
          success =
            "Password has been sucessfully changed! Redirecting to login...";
          await sleep(3000);

          goto(resolve("/login"));
        }
        loading = false;
      }}
    >
      Reset Password!</button
    >
  </fieldset>
</ModTogetherHero>
