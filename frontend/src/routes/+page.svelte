<script lang="ts">
	import TimetableComponent from '../components/TimetableComponent.svelte';

	import DaysOfWeekHeader from '../components/DaysOfWeekHeader.svelte';

	import Timeline from '../components/Timeline.svelte';
	import SearchBar from '../components/SearchBar.svelte';
	import { currentlySelectedMods, preferences } from '../shared/shared.svelte';
	import { getTimetable } from '../utils/format_db_information';

	const currentTimetableDisplay = $derived(
		getTimetable($preferences.acadYear, $preferences.currentSemView, $currentlySelectedMods)
	);
</script>

<SearchBar acadYear={$preferences.acadYear} semester={$preferences.currentSemView}></SearchBar>

<div class="flex">
	<button class="btn btn-primary" onclick={() => $preferences.currentSemView--}> Prev </button>
	<div class="text-center">Semester {$preferences.currentSemView}</div>
	<button class="btn btn-primary" onclick={() => $preferences.currentSemView++}> Next </button>
</div>

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
