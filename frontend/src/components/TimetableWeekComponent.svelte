<script lang="ts">
	import TimetableDayComponent from './TimetableDayComponent.svelte';

	import type { Module, RawLesson } from '../types/modules';
	import { currentlySelectedMods } from '../shared/shared.svelte';
	import type { TimeTableDayInfo } from '../types/internal';
	import { normaliseDuration } from '../utils/calculations_for_ui';

	interface WeekTimeTabledComponent {
		day: number;
		modInfo: { [moduleCode: string]: Module };
	}
	const { day, modInfo }: WeekTimeTabledComponent = $props();
	const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

	const filteredInformation: TimeTableDayInfo[] = $derived(
		calculateOverlappingTimes(filterByDay(modInfo))
	);

	function findOverlappingTimeInfo(itemToCompare: TimeTableDayInfo, allTimes: TimeTableDayInfo[]) {
		for (let i = 0; i < allTimes.length; i++) {
			const element = allTimes[i];
			if (itemToCompare.uniqueIdentifer == element.uniqueIdentifer) continue;

			if (
				element.normalisedStartDuration >= itemToCompare.normalisedStartDuration &&
				element.normalisedStartDuration < itemToCompare.normalisedEndDuration
			) {
				// means time has been found in between:
				itemToCompare.searchedModuleCodes.add(element.uniqueIdentifer);
				// // modify other one as well:
				// element.searchedModuleCodes = element.searchedModuleCodes.union(
				// 	itemToCompare.searchedModuleCodes
				// );
			}
		}
	}

	function calculateOverlappingTimes(timeTableInfo: TimeTableDayInfo[]): TimeTableDayInfo[] {
		for (let i = 0; i < timeTableInfo.length; i++) {
			const element = timeTableInfo[i];
			findOverlappingTimeInfo(element, timeTableInfo);
		}

		return timeTableInfo;
	}

	function filterByDay(modInfo: { [moduleCode: string]: Module }): TimeTableDayInfo[] {
		let totalInfo: TimeTableDayInfo[] = [];
		// For Displaying the Timetable:
		for (const mod in modInfo) {
			const info = modInfo[mod];

			const weekData = info.semesterData.find((semNo) => semNo.semester == 2);
			const ttData = weekData?.timetable.filter((x) => x.day == daysOfWeek[day]);

			const selectedMod = $currentlySelectedMods.selectedMods[mod];

			for (const lessonType in selectedMod) {
				const classNo = selectedMod[lessonType];
				const lessonForDay = ttData?.filter(
					(x) => x.lessonType == lessonType && x.classNo == classNo
				);

				if (lessonForDay?.length != 0) {
					const lesson = lessonForDay![0] as RawLesson;
					const identifer = `${lesson.classNo}-${info.moduleCode}`;
					totalInfo.push({
						lessonSchedule: lesson,
						moduleCode: info.moduleCode,
						moduleName: info.title,
						normalisedStartDuration: normaliseDuration('0800', '2000', lesson.startTime),
						normalisedEndDuration: normaliseDuration('0800', '2000', lesson.endTime),
						searchedModuleCodes: new Set<string>([identifer]),
						isAChoiceSelection: false,
						uniqueIdentifer: identifer
					});
				}
			}
		}

		const lessonQuery = modInfo['CS2030S'];

		const weekData = lessonQuery?.semesterData.find((semNo) => semNo.semester == 2);
		const ttData = weekData?.timetable.filter((x) => x.day == daysOfWeek[day]);
		const lessonTypeToMatch = ttData?.filter((x) => x.lessonType == 'Recitation');
		if (lessonTypeToMatch) {
			for (const lesson of lessonTypeToMatch!) {
				const identifer = `${lesson.classNo}-${lessonQuery.moduleCode}`;
				totalInfo.push({
					lessonSchedule: lesson,
					moduleCode: lessonQuery.moduleCode,
					moduleName: lessonQuery.title,
					normalisedStartDuration: normaliseDuration('0800', '2000', lesson.startTime),
					normalisedEndDuration: normaliseDuration('0800', '2000', lesson.endTime),
					searchedModuleCodes: new Set<string>([identifer]),
					isAChoiceSelection: true,
					uniqueIdentifer: identifer
				});
			}
		}

		return totalInfo;
	}
</script>

<div class="relative col-start-{day + 1} row-start-1">
	{#each filteredInformation as mod (mod)}
		<TimetableDayComponent
			lessonSchedule={mod.lessonSchedule}
			moduleCode={mod.moduleCode}
			moduleName={mod.moduleName}
			normalisedStartDuration={mod.normalisedStartDuration}
			normalisedEndDuration={mod.normalisedEndDuration}
			searchedModuleCodes={mod.searchedModuleCodes}
			isAChoiceSelection={mod.isAChoiceSelection}
			uniqueIdentifer={mod.uniqueIdentifer}
		></TimetableDayComponent>
	{/each}
</div>
