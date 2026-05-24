<script lang="ts">
	import TimetableDayComponent from './TimetableDayComponent.svelte';

	import type { Module, RawLesson } from '../types/modules';
	import { currentlySelectedMods, chooseModState, type LessonInfo } from '../shared/shared.svelte';
	import type { TimeTableDayInfo } from '../types/internal';
	import { normaliseDuration } from '../utils/calculations_for_ui';

	interface WeekTimeTabledComponent {
		day: number;
		modInfo: { [moduleCode: string]: Module };
	}
	const { day, modInfo }: WeekTimeTabledComponent = $props();
	const daysOfWeek = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'];

	const filteredInformation: TimeTableDayInfo[] = $derived(
		calculateOverlappingTimes(filterByDay(modInfo, chooseModState))
	);

	function findOverlappingTimeInfoNew(allTime: TimeTableDayInfo[]) {
		const groupTimes = Object.groupBy(allTime, (x) => x.normalisedStartDuration);
		const MAX_ITER = 1000;
		let iterIdx = 0;
		const processedTimings: string[] = [];
		const processedGroups: {
			[key: number]: TimeTableDayInfo[][];
		} = {};
		let lengthOfGroups = 0;
		for (const _ in groupTimes) lengthOfGroups++;
		let groupId = 0;
		while (processedTimings.length != lengthOfGroups && iterIdx != MAX_ITER) {
			iterIdx++;
			console.log(groupId);
			let firstGroup: TimeTableDayInfo[] = null;
			let firstGroupProcess: string = '';
			for (const i in groupTimes) {
				if (processedTimings.includes(i)) continue;
				firstGroup = groupTimes[i]!;
				firstGroupProcess = i;
				firstGroup.sort(
					(a, b) =>
						b.normalisedEndDuration -
						b.normalisedStartDuration -
						(a.normalisedEndDuration - a.normalisedStartDuration)
				);

				processedTimings.push(i);

				if (!processedGroups[groupId]) processedGroups[groupId] = [];
				processedGroups[groupId].push(firstGroup);
				break;
			}

			if (!firstGroup) break;
			let endTime = firstGroup[0].normalisedEndDuration;

			// Find groups:
			for (const i in groupTimes) {
				if (i == firstGroupProcess) continue;
				const group = groupTimes[i]![0];
				if (group.normalisedStartDuration >= endTime) {
					endTime = group.normalisedEndDuration;
					processedTimings.push(i);
					processedGroups[groupId].push(groupTimes[i]!);
				}
			}

			// Find Groups that may not exactly match endtimes:

			groupId++;
		}

		const outerGroupLength = Object.keys(processedGroups).length;

		for (const group in processedGroups) {
			const outerGroupIdx = Number.parseInt(group);

			for (const lessonGrouping of processedGroups[group]) {
				const innerGroupLength = lessonGrouping.length;
				for (let i = 0; i < lessonGrouping.length; i++) {
					const lesson = lessonGrouping[i];
					lesson.outerGroupIndex = outerGroupIdx;
					lesson.outerGroupLength = outerGroupLength;
					lesson.innerGroupLength = innerGroupLength;
					lesson.innerGroupIndex = i;
				}
			}
		}

		if (iterIdx == MAX_ITER) {
			console.log('Unable to find pairings');
		}
	}

	function calculateOverlappingTimes(timeTableInfo: TimeTableDayInfo[]): TimeTableDayInfo[] {
		findOverlappingTimeInfoNew(timeTableInfo);
		return timeTableInfo;
	}

	function filterByDay(
		modInfo: { [moduleCode: string]: Module },
		userState: LessonInfo
	): TimeTableDayInfo[] {
		const totalInfo: TimeTableDayInfo[] = [];
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
					const identifer = `${lesson.classNo}-${lesson.lessonType}-${info.moduleCode}`;
					totalInfo.push({
						lessonSchedule: lesson,
						moduleCode: info.moduleCode,
						moduleName: info.title,
						normalisedStartDuration: normaliseDuration('0800', '2000', lesson.startTime),
						normalisedEndDuration: normaliseDuration('0800', '2000', lesson.endTime),
						isAChoiceSelection: false,
						uniqueIdentifer: identifer,
						hasFoundAGroup: false,
						groupIndex: 0,
						innerGroupLength: 0
					});
				}
			}
		}
		const lessonQuery = modInfo[userState.moduleCode];

		const weekData = lessonQuery?.semesterData.find((semNo) => semNo.semester == 2);
		const ttData = weekData?.timetable.filter((x) => x.day == daysOfWeek[day]);
		const lessonTypeToMatch = ttData?.filter((x) => x.lessonType == userState.lessonType);
		if (lessonTypeToMatch) {
			for (const lesson of lessonTypeToMatch!) {
				if (lesson.classNo == userState.classNo) continue;
				const identifer = `${lesson.classNo}-${lesson.lessonType}-${lessonQuery.moduleCode}`;
				totalInfo.push({
					lessonSchedule: lesson,
					moduleCode: lessonQuery.moduleCode,
					moduleName: lessonQuery.title,
					normalisedStartDuration: normaliseDuration('0800', '2000', lesson.startTime),
					normalisedEndDuration: normaliseDuration('0800', '2000', lesson.endTime),
					isAChoiceSelection: true,
					uniqueIdentifer: identifer,
					hasFoundAGroup: false,
					groupIndex: 0,
					innerGroupLength: 0
				});
			}
		}

		totalInfo.sort((a, b) => a.normalisedStartDuration - b.normalisedStartDuration);

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
			isAChoiceSelection={mod.isAChoiceSelection}
			uniqueIdentifer={mod.uniqueIdentifer}
			innerGroupLength={mod.innerGroupLength}
			innerGroupIndex={mod.innerGroupIndex}
			outerGroupIndex={mod.outerGroupIndex}
			outerGroupLength={mod.outerGroupLength}
		></TimetableDayComponent>
	{/each}
</div>
