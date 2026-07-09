<script lang="ts">
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import ModTogetherHero from "$lib/components/LoginPage/ModTogetherHero.svelte";
  import { forgot_password } from "$lib/utils/db_operations";

  let emailInput = $state("");
  let loading = $state(false);
  let error = $state("");
  let success = $state("");
</script>

<ModTogetherHero>
  <fieldset
    class="fieldset bg-base-200 border-base-300 rounded-box w-xs border p-4"
  >
    <p class="text-error text-wrap break-after-all">
      {error}
    </p>
    <p class="text-success text-wrap break-after-all">{success}</p>
    <legend class="fieldset-legend">Reset Password!</legend>

    <label class="label">Email</label>
    <input
      type="email"
      bind:value={emailInput}
      class="input"
      placeholder="Email"
    />

    <button
      class="btn btn-primary mt-4 {loading ? 'btn-disabled' : ''}"
      onclick={async () => {
        loading = true;
        error = success = "";
        if (emailInput === "") {
          loading = false;
          error = "Please input email";
          return;
        }
        const result = await forgot_password(emailInput);

        if (result.isOk()) {
          success =
            "Reset Email request sent. If you have an account with us you will recieve an email within 5 minutes. Please check your spam as well.";
        } else {
          error = result.error;
        }
        loading = false;
      }}
    >
      Reset Password!</button
    >
    <button
      class="btn btn-secondary"
      onclick={() => {
        goto(resolve("/login"));
      }}>Back to login</button
    >
  </fieldset>
</ModTogetherHero>
