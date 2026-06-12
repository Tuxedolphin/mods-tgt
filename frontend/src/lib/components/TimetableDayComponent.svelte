<script lang="ts">
	import { currentlySelectedMods, chooseModState } from '$lib/shared/shared.svelte';
	import type { TimeTableDayInfo } from '$lib/types/internal';
	import { modifyModEntry } from '$lib/utils/format_db_information';

	interface TimetableDayProps {
		timeTableDayInfo: TimeTableDayInfo;
		semester: number;
		acadYear: string;
		timetable_id: string;
		timetable_name: string;
		timetable_colour: string;
	}
	const {
		timeTableDayInfo,
		acadYear,
		semester,
		timetable_id,
		timetable_name,
		timetable_colour
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
		if ($chooseModState.lessonType === '') {
			$chooseModState = {
				lessonType: timeTableDayInfo.lessonSchedule.lessonType,
				moduleCode: timeTableDayInfo.moduleCode,
				classNo: timeTableDayInfo.lessonSchedule.classNo,
				colour: timetable_colour
			};
		} else {
			currentlySelectedMods.set(
				await modifyModEntry(
					$currentlySelectedMods,
					acadYear,
					semester,
					timetable_id,
					timetable_name,
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
				moduleCode: ''
			};
		}
	}

	function styledAsPossibleSelection(): string {
		return timeTableDayInfo.isAChoiceSelection ? 'opacity-30' : 'opacity-100';
	}

	function calculateHeight(): string {
		return `h-${
			timeTableDayInfo.normalisedEndDuration * 192 - timeTableDayInfo.normalisedStartDuration * 192
		}`;
	}
	function calculateTopMargin(): string {
		return `mt-${timeTableDayInfo.normalisedStartDuration * 192}`;
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

	<div class="truncate">
		{timeTableDayInfo.lessonSchedule.lessonType}
	</div>
	<div class="opacity-50">
		[{timeTableDayInfo.lessonSchedule.classNo}]
	</div>
</div>
