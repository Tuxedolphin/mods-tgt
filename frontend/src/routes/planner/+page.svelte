<script lang="ts">
	import ModList from './ModListGroup.svelte';

	import DaysOfWeekHeader from '../../components/DaysOfWeekHeader.svelte';
	import SearchBar from '../../components/SearchBar.svelte';
	import Timeline from '../../components/Timeline.svelte';
	import TimetableComponent from '../../components/TimetableComponent.svelte';
	import {
		currentlySelectedMods,
		currentUserInformation,
		preferences
	} from '../../shared/shared.svelte';
	import { getTimetable } from '../../utils/format_db_information';

	let currentTimetableDisplay = $derived(
		getTimetable($preferences.acadYear, $preferences.currentSemView, $currentlySelectedMods)
	);
</script>

<div class="p-4">
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

	<ModList {currentTimetableDisplay}></ModList>
</div>
