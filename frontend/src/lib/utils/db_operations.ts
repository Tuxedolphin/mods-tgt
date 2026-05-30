import { PUBLIC_DB_LINK } from '$env/static/public';
import ky, { HTTPError } from 'ky';
import type {
	AuthResponse,
	AuthSucessResponse,
	ErrorInformation,
	ErrorResponse,
	TimetableInfos,
	TimetableWithMetadata,
	UserProfileResponse
} from '../types/db_raw_types';
import { Err, Ok, type Result } from 'ts-results-es';

const apiCalls = ky.create({
	baseUrl: PUBLIC_DB_LINK
});

export async function register_db(
	username: string,
	password: string
): Promise<Result<AuthSucessResponse, string>> {
	try {
		const json = await apiCalls
			.post('auth/register', {
				json: {
					email: username,
					password: password
				}
			})
			.json<AuthSucessResponse>();

		return new Ok(json);
	} catch (error) {
		try {
			if (error instanceof HTTPError) {
				const errorResponse = error.data as ErrorResponse;
				const errorMessage = JSON.parse(errorResponse.title) as ErrorInformation;
				return new Err(errorMessage.msg);
			}
		} catch {
			return new Err('Error Registering. Please try again');
		}

		return new Err('Error Registering. Please try again.');
	}
}

export async function login_to_db(
	username: string,
	password: string
): Promise<Result<AuthResponse, string>> {
	try {
		const json = await apiCalls
			.post('auth/login', {
				json: {
					email: username,
					password: password
				}
			})
			.json<AuthResponse>();
		return new Ok(json);
	} catch (error) {
		try {
			if (error instanceof HTTPError) {
				const errorResponse = error.data as ErrorResponse;
				const errorMessage = JSON.parse(errorResponse.title) as ErrorInformation;
				return new Err(errorMessage.msg);
			}
		} catch {
			return new Err('Wrong username or password');
		}

		return new Err('Wrong username or password');
	}
}

export async function put_user_info(
	access_token: string,
	username: string
): Promise<Result<string, string>> {
	try {
		await apiCalls.put('profile/me', {
			hooks: {
				beforeRequest: [
					({ request }) => {
						request.headers.set('Authorization', `Bearer ${access_token}`);
					}
				]
			},
			json: {
				username: username
			}
		});
		return Ok(username);
	} catch (error) {
		try {
			if (error instanceof HTTPError) {
				const errorResponse = error.data as ErrorResponse;
				const errorMessage = JSON.parse(errorResponse.title) as ErrorInformation;
				return new Err(errorMessage.msg);
			}
		} catch {
			return new Err('Wrong username or password');
		}

		return new Err('Wrong username or password');
	}
}

export async function get_user_info(
	access_token: string
): Promise<Result<UserProfileResponse, string>> {
	try {
		const timetables = await apiCalls
			.get('profile/me', {
				hooks: {
					beforeRequest: [
						({ request }) => {
							request.headers.set('Authorization', `Bearer ${access_token}`);
						}
					]
				}
			})
			.json<UserProfileResponse>();
		return Ok(timetables);
	} catch (error) {
		try {
			if (error instanceof HTTPError) {
				const errorResponse = error.data as ErrorResponse;
				const errorMessage = JSON.parse(errorResponse.title) as ErrorInformation;
				return new Err(errorMessage.msg);
			}
		} catch {
			return new Err('Wrong username or password');
		}

		return new Err('Wrong username or password');
	}
}

export async function get_timetables(
	access_token: string
): Promise<Result<TimetableInfos, string>> {
	try {
		const timetables = await apiCalls
			.get('/timetable', {
				hooks: {
					beforeRequest: [
						({ request }) => {
							request.headers.set('Authorization', `Bearer ${access_token}`);
						}
					]
				}
			})
			.json<TimetableInfos>();

		return Ok(timetables);
	} catch (error) {
		return Err('Something went wrong ' + error);
	}
}

export async function get_timetable_by_id(
	access_token: string,
	timetable_id: string
): Promise<Result<TimetableWithMetadata, string>> {
	try {
		const timetables = await apiCalls
			.get(`/timetable/${timetable_id}`, {
				hooks: {
					beforeRequest: [
						({ request }) => {
							request.headers.set('Authorization', `Bearer ${access_token}`);
						}
					]
				}
			})
			.json<TimetableWithMetadata>();
		return Ok(timetables);
	} catch (error) {
		return Err('Something went wrong ' + error);
	}
}

export async function put_timetable_by_id(
	access_token: string,
	timetable_id: string,
	timetable_data: TimetableWithMetadata
): Promise<Result<string, string>> {
	try {
		await apiCalls.put(`/timetable/${timetable_id}`, {
			hooks: {
				beforeRequest: [
					({ request }) => {
						request.headers.set('Authorization', `Bearer ${access_token}`);
					}
				]
			},
			json: timetable_data
		});
		return Ok('');
	} catch (error) {
		return Err('Something went wrong ' + error);
	}
}
