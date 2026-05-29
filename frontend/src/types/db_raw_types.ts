export interface AuthResponse {
	access_token: string;
	refresh_token: string;
	expires_in: number;
	token_type: string;
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
