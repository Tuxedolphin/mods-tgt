<script lang="ts">
	import DaysOfWeekHeader from '$lib/components/DaysOfWeekHeader.svelte';
	import ModListGroup from '$lib/components/ModListGroup.svelte';
	import SearchBar from '$lib/components/SearchBar.svelte';
	import Timeline from '$lib/components/Timeline.svelte';
	import TimetableComponent from '$lib/components/TimetableComponent.svelte';
	import {
		preferences,
		currentlySelectedMods,
		currentUserInformation
	} from '$lib/shared/shared.svelte';
	import { getTimetable } from '$lib/utils/format_db_information';

	let currentTimetableDisplay = $derived(
		getTimetable($preferences.acadYear, $preferences.currentSemView, $currentlySelectedMods)
	);
</script>

<div class="text-2xl">Welcome, {$currentUserInformation.displayName}</div>
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
		<TimetableComponent
			timetables={currentTimetableDisplay}
			acadYear={$preferences.acadYear}
			semester={$preferences.currentSemView}
		></TimetableComponent>
	</div>
</div>

<ModListGroup {currentTimetableDisplay}></ModListGroup>
