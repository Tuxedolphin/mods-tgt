<script lang="ts">
  import { onMount } from "svelte";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import ModTogetherHero from "$lib/components/LoginPage/ModTogetherHero.svelte";

  let token_hash = $state("");
  let type = $state("");
  let passwordInput = $state("");
  let confirmPasswordInput = $state("");
  let loading = $state(false);
  let error = $state("");
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
  <p class="text-error">{error}</p>
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
      onclick={() => {
        loading = true;

        loading = false;
      }}
    >
      Reset Password!</button
    >
  </fieldset>
</ModTogetherHero>
