<script lang="ts">
  import { users_to_hide } from "$lib/shared/shared.svelte";
  import type { Profile } from "$lib/types/db_raw_types";
  import { Eye, EyeClosed } from "@lucide/svelte";
  import { remove } from "es-toolkit";

  let is_visible = $state(true);

  interface VisibilityComponentProps {
    profile: Profile;
  }

  let { profile }: VisibilityComponentProps = $props();
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
  onclick={() => {
    is_visible = !is_visible;

    if (!is_visible) {
      const curr_users = $users_to_hide;
      curr_users.push(profile.userId);

      users_to_hide.set(curr_users);
    } else {
      const curr_users = $users_to_hide;
      remove(curr_users, (x) => x === profile.userId);

      users_to_hide.set(curr_users);
    }
  }}
>
  {#if is_visible}
    <Eye></Eye>
  {:else}
    <EyeClosed></EyeClosed>
  {/if}
</div>
