<script lang="ts">
  import { onMount } from "svelte";
  import { get } from "svelte/store";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import {
    currentUserInformation,
    token_information,
  } from "$lib/shared/shared.svelte";
  import { get_user_info } from "$lib/utils/db_operations";

  // This component just checks if there's any token, attempts to login, and
  // redirects to home if possible.

  onMount(async () => {
    const token = get(token_information).a;
    if (token) {
      const user_info = await get_user_info(token);

      if (user_info.isOk()) {
        const info = user_info.value;
        $currentUserInformation.avatarUrl = info.avatarUrl;
        $currentUserInformation.userId = info.userId;
        $currentUserInformation.username = info.username;

        goto(resolve("/(app)/home"));
      }
    }
  });
</script>
