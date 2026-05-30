// Returns semester numbers as "Special term I" and "Special term II"
// if 3 and 4
export function format_semester_name(number: number): string {
	let semester_name = '';
	switch (number) {
		case 1:
		case 2:
			semester_name = `Semester ${number}`;
			break;
		case 3:
		case 4:
			semester_name = `Special Term ${number - 2}`;
			break;
		default:
			break;
	}

	return semester_name;
}
