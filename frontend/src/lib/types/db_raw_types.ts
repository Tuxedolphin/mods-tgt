export interface AuthResponse {
	accessToken: string;
	refreshToken: string;
	expiresIn: number;
	tokenType: string;
}

export interface UserProfileResponse {
	username: string | null;
}

export interface AuthSucessResponse {
	message: string;
}

export interface ErrorResponse {
	type: string;
	title: string;
	status: number;
}

export interface ErrorInformation {
	code: number;
	error_code: string;
	msg: string;
}

export type TimetableInfos = TimetableInfo[];

export interface TimetableInfo {
	id: string;
	name: string;
	semester: number;
	academicYear: string;
	createdAt: string;
}

export interface TimetableWithMetadata extends TimetableInfo {
	metaData: TimetableLessonMetadata[];
}

export interface TimetableLessonMetadata {
	moduleCode: string;
	lessonNo: string;
	lessonType: string;
}

export interface TimetablePostTemplate {
	name: string;
	semester: number;
	academicYear: string;
	metaData: never[]
}
