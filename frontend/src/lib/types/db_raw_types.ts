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
