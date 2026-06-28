<script lang="ts">
	import {
		currentlySelectedMods,
		chooseModState,
		currentUserInformation
	} from '$lib/shared/shared.svelte';
	import type { TimeTableDayInfo } from '$lib/types/internal';
	import { modifyModEntry } from '$lib/utils/format_db_information';

	interface TimetableDayProps {
		timeTableDayInfo: TimeTableDayInfo;
		semester: number;
		acadYear: string;
		timetable_id: string;
		timetable_colour: string;
		height_of_one_hour_lesson: number;
	}
	const {
		timeTableDayInfo,
		acadYear,
		semester,
		timetable_id,
		timetable_colour,
		height_of_one_hour_lesson
	}: TimetableDayProps = $props();

	const spaceAllowedToUse = $derived(100.0 / timeTableDayInfo.outerGroupLength);
	const startingOuterOffset = $derived(timeTableDayInfo.outerGroupIndex * spaceAllowedToUse);
	const leftMarginPercentage = $derived(
		timeTableDayInfo.innerGroupIndex * (spaceAllowedToUse / timeTableDayInfo.innerGroupLength) +
			startingOuterOffset
	);
	const showModName = $state(false);
	const width = $derived(spaceAllowedToUse / timeTableDayInfo.innerGroupLength);

	async function changeTimetable() {
		if (
			$currentUserInformation.userId !== timeTableDayInfo.timetableOwner?.userId &&
			!timeTableDayInfo.isAChoiceSelection
		)
			return;
		if ($chooseModState.lessonType === '') {
			$chooseModState = {
				lessonType: timeTableDayInfo.lessonSchedule.lessonType,
				moduleCode: timeTableDayInfo.moduleCode,
				classNo: timeTableDayInfo.lessonSchedule.classNo,
				colour: timetable_colour,
				selectedTimetableId: timetable_id
			};
		} else {
			currentlySelectedMods.set(
				await modifyModEntry(
					$currentlySelectedMods,
					acadYear,
					semester,
					timetable_id,
					timeTableDayInfo.moduleCode,
					timeTableDayInfo.lessonSchedule.lessonType,
					timeTableDayInfo.lessonSchedule.classNo,
					$chooseModState
				)
			);
			$chooseModState = {
				classNo: '',
				colour: '',
				lessonType: '',
				moduleCode: '',
				selectedTimetableId: ''
			};
		}
	}

	function styledAsPossibleSelection(): string {
		return timeTableDayInfo.isAChoiceSelection ? 'opacity-30' : 'opacity-100';
	}

	// svelte-ignore state_referenced_locally
	const pixel_conversion = 12 * height_of_one_hour_lesson;

	function calculateHeight(): string {
		const rounded_height = Math.round(
			timeTableDayInfo.normalisedEndDuration * pixel_conversion -
				timeTableDayInfo.normalisedStartDuration * pixel_conversion
		);
		return `h-${rounded_height}`;
	}
	function calculateTopMargin(): string {
		const rounded_top_margin = Math.round(
			timeTableDayInfo.normalisedStartDuration * pixel_conversion
		);
		return `mt-${rounded_top_margin}`;
	}
</script>

<!-- svelte-ignore a11y_click_events_have_key_events -->
<!-- svelte-ignore a11y_no_static_element_interactions -->
<div
	style:margin-left="{leftMarginPercentage}%"
	style:width="{width}%;"
	class="absolute
	rounded
	p-1
	{calculateHeight()}
	{styledAsPossibleSelection()}
	{calculateTopMargin()}  
	{timetable_colour} 
	text-[10px]
	wrap-break-word
	text-black
	md:text-xs"
	onclick={async () => changeTimetable()}
>
	<div class="font-semibold">
		{timeTableDayInfo.moduleCode}
		{showModName ? timeTableDayInfo.moduleName : ''}
	</div>

	<div class="flex gap-1">
		<div class="truncate">
			{timeTableDayInfo.lessonSchedule.lessonType}
		</div>
		<div class="opacity-50">
			[{timeTableDayInfo.lessonSchedule.classNo}]
		</div>
	</div>

	<div class="text-[10px] italic">
		{#if timeTableDayInfo.timetableOwner?.userId === $currentUserInformation.userId}
			{timeTableDayInfo.timetableOwner?.username} (You)
		{:else}
			{timeTableDayInfo.timetableOwner?.username}
		{/if}
	</div>
</div>
