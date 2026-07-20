<script lang="ts">
  import { onMount } from "svelte";
  import { currentUserInformation } from "$lib/shared/shared.svelte";

  let handle = $state("");
  onMount(async () => {
    handle = $currentUserInformation.username!;
  });
</script>

<div class="text-xl font-semibold text-ellipsis">Welcome, {handle}</div>
