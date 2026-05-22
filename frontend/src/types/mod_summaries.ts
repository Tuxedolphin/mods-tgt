export interface ModSummary {
	moduleCode: string;
	title: string;
	semesters: number[];
}

export type SavedModInfo = {
	[lessonType: string]: string;
};
