<script lang="ts">
	import { searchTerm } from '$lib/shared/shared.svelte';

	import type { ModSummary } from '$lib/types/mod_summaries';

	import { getListOfModsSummary } from '$lib/utils/fetch_from_cache';

	import { AllSubstringsIndexStrategy, Search } from 'js-search';
	import { onDestroy, onMount } from 'svelte';
	import ModuleCodeSuggestionMini from './ModuleCodeSuggestionMini.svelte';

	interface SearchBarProps {
		acadYear: string;
		semester: number;
		timetable_id: string;
	}
	const { acadYear, semester, timetable_id }: SearchBarProps = $props();
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

	onDestroy(() => {
		searchTerm.set('');
	});
</script>

<input
	type="text"
	placeholder="Search for a mod here"
	class="input mx-1 w-full"
	bind:value={$searchTerm}
/>

<div class="max-h-36 overflow-auto scroll-auto">
	{#each results as res (res.moduleCode)}
		<ModuleCodeSuggestionMini {timetable_id} mod={res} {semester} {acadYear}
		></ModuleCodeSuggestionMini>
	{/each}
</div>
