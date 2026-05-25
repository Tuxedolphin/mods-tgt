<script lang="ts">
	import TimetableComponent from '../components/TimetableComponent.svelte';

	import DaysOfWeekHeader from '../components/DaysOfWeekHeader.svelte';

	import Timeline from '../components/Timeline.svelte';
	import SearchBar from '../components/SearchBar.svelte';
	import { onMount } from 'svelte';
	import type { ModSummary } from '../types/mod_summaries';
	import { getListOfModsSummary } from '../utils/fetch_from_cache';
	import { preferences } from '../shared/shared.svelte';

	let modData = $state([]) as ModSummary[];
	onMount(async () => {
		modData = await getListOfModsSummary($preferences.acadYear);
		// Setup Preferences:
	});
</script>

{#if modData.length != 0}
	<SearchBar summaries={modData}></SearchBar>

	<div class="flex">
		<button class="btn-primary btn" onclick={() => $preferences.currentSemView--}> Prev </button>
		<div class="text-center">Semester {$preferences.currentSemView}</div>
		<button class="btn-primary btn" onclick={() => $preferences.currentSemView++}> Next </button>
	</div>
{/if}

<div class="flex">
	<Timeline></Timeline>
	<div class="flex-1 flex-col">
		<DaysOfWeekHeader></DaysOfWeekHeader>
		<TimetableComponent></TimetableComponent>
	</div>
</div>
