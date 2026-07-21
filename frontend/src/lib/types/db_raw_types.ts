export type RoomRole = "owner" | "editor" | "viewer";

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  tokenType: string;
}

export interface HandleAvailabilityResponse {
  available: boolean;
  reason:
    | null
    | "taken"
    | "reserved"
    | "invalidFormat"
    | "tooShort"
    | "tooLong";
}

export interface Profile {
  userId: string;
  username: string | null;
  avatarUrl: string | null;
  handle: string | null;
  colour: string | null;
  defaultTheme: string | null;
}

export interface RoomProfile extends Profile {
  role: RoomRole;
  isInRoom: boolean;
}

export interface AuthSucessResponse {
  message: string;
}

export interface ErrorResponse {
  type: string;
  title: string;
  status: number;
}

export interface ProfileValidationErrorResponse extends ErrorResponse {
  errors: ProfileValidationErrors;
}

export interface ProfileValidationErrors {
  Handle: string[];
  Username: string[];
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
  members: RoomProfile[];
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
