export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

export interface Profile {
  userId: string;
  username: string | null;
  avatarUrl: string | null;
  handle: string | null;
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

export type TimetableInfos = TimetableSummaryResponse[];

export interface TimetableSummaryResponse {
  id: string;
  name: string;
  semester: number;
  academicYear: string;
  createdAt: string;
}

export interface TimetableResponse extends TimetableSummaryResponse {
  metaData: TimetableModule[];
}

export interface RoomInformation {
  roomId: string;
  users: Profile[];
  timetables: TimetableDetailedResponse[];
}

export interface TimetableDetailedResponse extends TimetableResponse {
  profile: Profile;
}

export interface TimetableModule {
  moduleCode: string;
  lessonNo: string;
  lessonType: string;
  colour: string;
}

export interface TimetablePostTemplate {
  name: string;
  semester: number;
  academicYear: string;
  metaData: never[];
}
