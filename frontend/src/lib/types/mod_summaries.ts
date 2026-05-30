export interface ModSummary {
	moduleCode: string;
	title: string;
	semesters: number[];
}

export type SavedModInfo = {
	[lessonType: string]: string;
};

export type TimeTable = {
	Owner: string;
	Name: string;
	Semester: number;
	AcademicYear: string;
	LessonData: LessonData[];
};

export type LessonData = {
	ModuleCode: string;
	LessonNo: string;
	LessonType: string;
};
