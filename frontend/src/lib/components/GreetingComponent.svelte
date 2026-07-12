<script lang="ts">
  import { onMount } from "svelte";
  import { currentUserInformation } from "$lib/shared/shared.svelte";

  let username = $state("");
  onMount(async () => {
    username = $currentUserInformation.username!;
  });
</script>

<div class="text-xl font-semibold text-ellipsis">Welcome, {username}</div>
