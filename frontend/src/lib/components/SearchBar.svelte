<script lang="ts">
  import { AllSubstringsIndexStrategy, Search } from "js-search";
  import { onDestroy, onMount } from "svelte";
  import { searchTerm } from "$lib/shared/shared.svelte";
  import type { ModSummary } from "$lib/types/mod_summaries";
  import { getListOfModsSummary } from "$lib/utils/fetch_from_cache";
  import ModuleCodeSuggestionMini from "./ModuleCodeSuggestionMini.svelte";

  interface SearchBarProps {
    acadYear: string;
    semester: number;
    timetable_id: string;
  }
  const { acadYear, semester, timetable_id }: SearchBarProps = $props();
  let modData = $state([]) as ModSummary[];
  let loading = $state(false);
  const modSearch = new Search("moduleCode");

  onMount(async () => {
    loading = true;
    modData = await getListOfModsSummary(acadYear);
    modSearch.indexStrategy = new AllSubstringsIndexStrategy();
    modSearch.addIndex("moduleCode");
    modSearch.addIndex("title");
    modSearch.addDocuments(modData as ModSummary[]);
    loading = false;
  });

  let results = $derived(
    $searchTerm.length < 3
      ? []
      : // eslint-disable-next-line @typescript-eslint/no-unused-vars
        (modSearch.search($searchTerm) as ModSummary[]).sort((a, _) =>
          a.semesters.includes(semester) ? -1 : 1,
        ),
  );

  onDestroy(() => {
    searchTerm.set("");
  });
</script>

{#if loading}
  <input
    disabled
    type="text"
    placeholder="Loading..."
    class="input mx-1 w-full input-ghost"
  />
{:else}
  <input
    type="text"
    placeholder="Enter Module Code or Module Name..."
    class="input w-full input-primary"
    bind:value={$searchTerm}
  />
{/if}

<div class="max-h-36 overflow-auto scroll-auto">
  {#each results as res (res.moduleCode)}
    <ModuleCodeSuggestionMini {timetable_id} mod={res} {semester} {acadYear}
    ></ModuleCodeSuggestionMini>
  {/each}
</div>
