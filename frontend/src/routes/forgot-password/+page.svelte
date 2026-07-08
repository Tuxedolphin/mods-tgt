<script lang="ts">
  import ModTogetherHero from "$lib/components/LoginPage/ModTogetherHero.svelte";
  import type { PageProps } from "./$types";

  let emailInput = $state("");
  let loading = $state(false);
  let error = $state("");
</script>

<ModTogetherHero>
  <p class="text-error">{error}</p>
  <fieldset
    class="fieldset bg-base-200 border-base-300 rounded-box w-xs border p-4"
  >
    <legend class="fieldset-legend">Reset Password!</legend>

    <label class="label">Email</label>
    <input
      type="email"
      bind:value={emailInput}
      class="input"
      placeholder="Email"
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
