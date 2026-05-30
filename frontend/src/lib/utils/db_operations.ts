import { PUBLIC_DB_LINK } from '$env/static/public';
import ky, { HTTPError } from 'ky';
import type {
	AuthResponse,
	AuthSucessResponse,
	ErrorInformation,
	ErrorResponse
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
				console.log(error.data);
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
				console.log(error.data);
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

export async function get_timetables(access_token: string) {
	const timetables = await apiCalls.get('/timetable', {
		hooks: {
			beforeRequest: [
				({ request }) => {
					request.headers.set('Authorization', `Bearer ${access_token}`);
				}
			]
		}
	});
}
