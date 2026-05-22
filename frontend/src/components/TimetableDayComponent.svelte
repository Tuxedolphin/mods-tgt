<script lang="ts">
	import type { TimeTableDayInfo } from '../types/internal';

	const { lessonSchedule, moduleName, moduleCode }: TimeTableDayInfo = $props();
	const startTime = () => lessonSchedule.startTime;
	const endTime = () => lessonSchedule.endTime;

	function parse24HourTimeToMin(time24Hour: string): number {
		const startHour = Number.parseInt(time24Hour.substring(0, 2));
		const startMin = Number.parseInt(time24Hour.substring(2, 4));

		return startHour * 60 + startMin;
	}

	function parseDiffDuration(startTime24Hour: string, endTime24Hour: string): number {
		const startMins = parse24HourTimeToMin(startTime24Hour);
		const endMins = parse24HourTimeToMin(endTime24Hour);
		return endMins - startMins;
	}

	function calculateHeightOfClass(
		startTime24Hour: string,
		endTime24Hour: string,
		fullHourUnitsInPx: number
	): number {
		const diff = parseDiffDuration(startTime24Hour, endTime24Hour);
		return (diff / 60.0) * fullHourUnitsInPx;
	}
</script>

<div
	class="absolute mt-{calculateHeightOfClass(
		'0800',
		startTime(),
		16
	)} w-full h-{calculateHeightOfClass(startTime(), endTime(), 16)} bg-amber-100"
>
	{moduleCode}
</div>
