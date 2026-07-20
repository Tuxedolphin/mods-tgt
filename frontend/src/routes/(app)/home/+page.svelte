<script lang="ts">
  import { onMount } from "svelte";
  import { goto } from "$app/navigation";
  import { resolve } from "$app/paths";
  import AvailableTimetableGrid from "$lib/components/AvailableTimetableGrid.svelte";
  import GreetingComponent from "$lib/components/GreetingComponent.svelte";
  import ImportFromNUSMods from "$lib/components/ImportFromNUSMods.svelte";
  import { token_information } from "$lib/shared/shared.svelte";
  import CreateNewTimetableButton from "../../../lib/components/CreateNewTimetableButton.svelte";

  let token = $state("");
  onMount(() => {
    if (!$token_information.a) {
      goto(resolve("/login"));
    }

    token = $token_information.a;
  });
</script>

{#if token}
  <div class="flex items-center justify-between mt-2">
    <GreetingComponent></GreetingComponent>
    <div class="flex gap-2">
      <CreateNewTimetableButton></CreateNewTimetableButton>
      <ImportFromNUSMods></ImportFromNUSMods>
    </div>
  </div>

  <hr class="my-2 h-px border-0 bg-gray-200" />

  <AvailableTimetableGrid></AvailableTimetableGrid>
{/if}
