<script lang="ts">
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import {
    currentUserInformation,
    currentWorkingTimetable,
    token_information,
  } from "$lib/shared/shared.svelte";
  import { get_user_info, login_to_db } from "$lib/utils/db_operations";

  interface LoginButtonProps {
    email: string;
    password: string;
    errorMessage: string;
  }
  let loading = $state(false);

  let {
    errorMessage = $bindable<string>(),
    email,
    password,
  }: LoginButtonProps = $props();

  async function login() {
    const urlParams = new URLSearchParams(window.location.search);
    loading = true;
    const result = await login_to_db(email, password);
    if (result.isOk()) {
      // Stores access token in localstorage (FOR NOW) -- Not secure:!
      $token_information.a = result.value.accessToken;
      $token_information.b = false;

      const tt = await get_user_info(result.value.accessToken);

      if (tt.isOk()) {
        const result = tt.value;
        // if (!tt.value.username) {
        //
        // } else {
        //   $currentUserInformation.username = tt.value.username;
        //   $currentUserInformation.userId = tt.value.userId;
        //   $currentUserInformation.handle = tt.value.handle;
        // }

        // this is for v2.0.1 and before, handles & colour were not added:

        if (result.username && (!result.handle || !result.colour)) {
          // Welcome user back, and change their stuff
          await goto(resolve("/welcome?action=back"));
          return;
        }

        // this is default: the user does not have any information
        if (!tt.value.username) {
          console.log("New User");
          await goto(resolve("/welcome"));
          return;
        }
      }

      if (urlParams.get("action") === "redirect") {
        currentWorkingTimetable.reset();
        const tt_id = urlParams.get("tt_id")!;
        console.log("Redirect to: " + tt_id);
        goto(
          resolve("/(app)/planner/[timetable_id]", {
            timetable_id: tt_id,
          }),
        );

        return;
      }

      goto(resolve("/home"));
    } else {
      errorMessage = result.error;
    }
    loading = false;
  }
</script>

{#if loading}
  <button class="btn btn-disabled mt-4 btn-neutral" disabled type="submit"
    >Logging in...</button
  >
{:else}
  <button
    class="btn mt-4 btn-neutral"
    type="submit"
    onclick={async () => {
      await login();
    }}>Login</button
  >
{/if}
