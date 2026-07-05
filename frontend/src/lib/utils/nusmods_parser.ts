import type { TimetableModule } from '$lib/types/db_raw_types';
import { Err, Ok, type Result } from 'ts-results-es';
import { get_random_colour, get_randomised_colour } from './formatting_utils';

// Taken from NUS Mods Source Code:
type lessonTypeAbbrev = { [lessonType: string]: string };

const LESSON_TYPE_ABBREV: lessonTypeAbbrev = {
	'Design Lecture': 'DLEC',
	Laboratory: 'LAB',
	Lecture: 'LEC',
	'Packaged Laboratory': 'PLAB',
	'Packaged Lecture': 'PLEC',
	'Packaged Tutorial': 'PTUT',
	Recitation: 'REC',
	'Sectional Teaching': 'SEC',
	'Seminar-Style Module Class': 'SEM',
	Tutorial: 'TUT',
	'Tutorial Type 2': 'TUT2',
	'Tutorial Type 3': 'TUT3',
	Workshop: 'WS'
};

const ABBREV_TO_LESSON_TYPE: lessonTypeAbbrev = {
	DLEC: 'Design Lecture',
	LAB: 'Laboratory',
	LEC: 'Lecture',
	PLAB: 'Packaged Laboratory',
	PLEC: 'Packaged Lecture',
	PTUT: 'Packaged Tutorial',
	REC: 'Recitation',
	SEC: 'Sectional Teaching',
	SEM: 'Seminar-Style Module Class',
	TUT: 'Tutorial',
	TUT2: 'Tutorial Type 2',
	TUT3: 'Tutorial Type 3',
	WS: 'Workshop'
};

export function parse_mods_link(link: string): Result<string, string> {
	let sem_number = 0;

	const url = URL.parse(link);

	if (!url) return Err('Missing Url / Problematic Url!');

	const path_name = url?.pathname.split('/');

	// Look for sem- / st-?
	const sem = path_name.find((x) => x.startsWith('sem') || x.startsWith('st'));

	if (!sem) return Err('Missing semester information / Problematic Url');

	const sem_number_result = parse_sem_string(sem);

	if (sem_number_result.isErr()) return Err(sem_number_result.error);

	sem_number = sem_number_result.value;
	// Sample:
	// https://nusmods.com/timetable/sem-1/share?CS2030S=REC:02,LAB:12A,LEC:1&CS2040S=LAB:12,LEC:1
	const timetable_modules: TimetableModule[] = [];

	// Sample module_classes: LAB:12,LEC:1
	for (const [module_code, module_classes] of url.searchParams) {
		const colour = get_random_colour();
		const classes = module_classes.split(',');

		for (const curr_class of classes) {
			const [lesson_type, lesson_no] = curr_class.split(':');

			timetable_modules.push({
				colour: colour,
				lessonNo: lesson_no,
				moduleCode: module_code,
				lessonType: ABBREV_TO_LESSON_TYPE[lesson_type]
			});
		}
	}

	console.log(timetable_modules);
	return Ok('');
}

// Takes a sem-string (i.e., sem-1, sem-2, st-i) and returns the
// semester number
function parse_sem_string(sem_string: string): Result<number, string> {
	const sem_number = sem_string.split('-');

	if (sem_number.length != 2) return Err('Invalid/Empty String');

	if (sem_number[1] === '1') return Ok(1);
	if (sem_number[1] === '2') return Ok(2);
	if (sem_number[1] === 'i') return Ok(3);
	if (sem_number[1] === 'ii') return Ok(4);

	return Err('Invalid String');
}
