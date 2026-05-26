<script lang="ts">
	import ModuleCodeSuggestionMini from './ModuleCodeSuggestionMini.svelte';

	import { AllSubstringsIndexStrategy, Search } from 'js-search';
	import type { ModSummary } from '../types/mod_summaries';
	import { onMount } from 'svelte';
	import { searchTerm } from '../shared/shared.svelte';
	import { getListOfModsSummary } from '../utils/fetch_from_cache';
	interface SearchBarProps {
		acadYear: string;
		semester: number;
	}
	const { acadYear, semester }: SearchBarProps = $props();
	let modData = $state([]) as ModSummary[];
	const modSearch = new Search('moduleCode');

	onMount(async () => {
		modData = await getListOfModsSummary(acadYear);
		modSearch.indexStrategy = new AllSubstringsIndexStrategy();
		modSearch.addIndex('moduleCode');
		modSearch.addIndex('title');
		modSearch.addDocuments(modData as ModSummary[]);
	});

	let results = $derived(
		$searchTerm.length < 3
			? []
			: // eslint-disable-next-line @typescript-eslint/no-unused-vars
				(modSearch.search($searchTerm) as ModSummary[]).sort((a, _) =>
					a.semesters.includes(semester) ? -1 : 1
				)
	);
</script>

<p>Search</p>
<input type="text" placeholder="Type here" class="input w-full" bind:value={$searchTerm} />

<div class="max-h-36 overflow-auto scroll-auto">
	{#each results as res (res.moduleCode)}
		<ModuleCodeSuggestionMini mod={res}></ModuleCodeSuggestionMini>
	{/each}
</div>
