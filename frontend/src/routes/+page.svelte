<script lang="ts">
	import TimetableComponent from '../components/TimetableComponent.svelte';

	import DaysOfWeekHeader from '../components/DaysOfWeekHeader.svelte';

	import Timeline from '../components/Timeline.svelte';
	import SearchBar from '../components/SearchBar.svelte';
	import { onMount } from 'svelte';
	import type { ModSummary } from '../types/mod_summaries';
	import { getListOfModsSummary } from '../utils/fetch_from_cache';
	import { currentlySelectedMods, preferences } from '../shared/shared.svelte';
	import { getTimetable } from '../utils/format_db_information';

	let modData = $state([]) as ModSummary[];
	onMount(async () => {
		modData = await getListOfModsSummary($preferences.acadYear);
		// Setup Preferences:
		console.log('Startup: ' + currentTimetableDisplay);
	});

	const currentTimetableDisplay = $derived(
		getTimetable($preferences.acadYear, $preferences.currentSemView, $currentlySelectedMods)
	);
</script>

{#if modData.length != 0}
	<SearchBar summaries={modData}></SearchBar>

	<div class="flex">
		<button class="btn btn-primary" onclick={() => $preferences.currentSemView--}> Prev </button>
		<div class="text-center">Semester {$preferences.currentSemView}</div>
		<button class="btn btn-primary" onclick={() => $preferences.currentSemView++}> Next </button>
	</div>
{/if}

<div class="flex">
	<Timeline></Timeline>
	<div class="flex-1 flex-col">
		<DaysOfWeekHeader></DaysOfWeekHeader>
		<TimetableComponent timetables={currentTimetableDisplay}></TimetableComponent>
	</div>
</div>

<div>
	{#each currentTimetableDisplay as tt (tt.AcademicYear)}
		Timetable Belongs to: {tt.Name}
	{/each}
</div>
