export function parse24HourTimeToMin(time24Hour: string): number {
	const startHour = Number.parseInt(time24Hour.substring(0, 2));
	const startMin = Number.parseInt(time24Hour.substring(2, 4));

	return startHour * 60 + startMin;
}

export function parseDiffDuration(startTime24Hour: string, endTime24Hour: string): number {
	const startMins = parse24HourTimeToMin(startTime24Hour);
	const endMins = parse24HourTimeToMin(endTime24Hour);
	return endMins - startMins;
}

export function calculateHeightOfClass(
	startTime24Hour: string,
	endTime24Hour: string,
	fullHourUnitsInPx: number
): number {
	const diff = parseDiffDuration(startTime24Hour, endTime24Hour);
	return (diff / 60.0) * fullHourUnitsInPx;
}

// Takes a start and end time, returns 0-1
// norm based on time given.
// i.e: start: 0800, end: 0900, timeToNorm: 0830,
// returns 0.5;
export function normaliseDuration(
	startTime24Hour: string,
	endTime24Hour: string,
	timeToNormalise: string
): number {
	const fullDiff = parseDiffDuration(startTime24Hour, endTime24Hour);
	const diff = parseDiffDuration(startTime24Hour, timeToNormalise);

	return diff / fullDiff;
}
