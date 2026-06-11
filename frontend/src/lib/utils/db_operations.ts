import { PUBLIC_DB_LINK } from '$env/static/public';
import ky, { HTTPError } from 'ky';
import type {
	AuthResponse,
	AuthSucessResponse,
	ErrorInformation,
	ErrorResponse,
	TimetableInfo,
	TimetableInfos,
	TimetablePostTemplate,
	TimetableWithMetadata,
	UserProfileResponse
} from '../types/db_raw_types';
import { Err, Ok, type Result } from 'ts-results-es';
import { goto } from '$app/navigation';
import { resolve } from '$app/paths';
import { token_information, currentUserInformation } from '$lib/shared/shared.svelte';
import { getFromSessionStorage, storeInfoSessionStorage } from './fetch_from_cache';
import { get } from 'svelte/store';

const apiCalls = ky.create({
	baseUrl: PUBLIC_DB_LINK
});

type CustomOptions = {
	authorised?: boolean;
	unauthorizedCheck?: boolean;
	auth_token?: string;
};

function create_ky_instance(custom_options: CustomOptions) {
	return apiCalls.extend({
		hooks: {
			beforeRequest: [
				({ request }) => {
					if (custom_options.authorised) {
						request.headers.set('Authorization', `Bearer ${custom_options.auth_token}`);
					}
				}
			],
			afterResponse: [
				async ({ response }) => {
					if (custom_options.unauthorizedCheck && response.status === 401) {
						token_information.reset();
						const message = 'Login expired, please login in again';
						goto(resolve(`/login#error_description=${message}`));
					}
				}
			]
		}
	});
}

export async function register_db(
	username: string,
	password: string
): Promise<Result<AuthSucessResponse, string>> {
	try {
		const register = create_ky_instance({ authorised: false, unauthorizedCheck: false }).extend({
			json: {
				email: username,
				password: password
			}
		});
		const register_json = await register.post('auth/register').json<AuthSucessResponse>();

		return new Ok(register_json);
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
		const login_db = create_ky_instance({ authorised: false, unauthorizedCheck: false }).extend({
			json: {
				email: username,
				password: password
			}
		});
		const login_json = await login_db.post('auth/login').json<AuthResponse>();
		return new Ok(login_json);
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
		const put_user_db = create_ky_instance({
			authorised: true,
			unauthorizedCheck: true,
			auth_token: access_token
		}).extend({
			json: {
				username: username
			}
		});
		await put_user_db.put('profile/me');
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
	//const name = currentUserInformation
	const user_info = get(currentUserInformation);
	
	if (user_info.username) {
		return Ok(user_info);
	}
	try {
		const get_user_info_db = create_ky_instance({
			authorised: true,
			unauthorizedCheck: true,
			auth_token: access_token
		});
		const timetables = await get_user_info_db.get('profile/me').json<UserProfileResponse>();

		currentUserInformation.set(timetables);
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
		const get_timetables_db = create_ky_instance({
			authorised: true,
			unauthorizedCheck: true,
			auth_token: access_token
		});
		const timetables = await get_timetables_db.get('/timetable').json<TimetableInfos>();
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
		const get_timetables_id_db = create_ky_instance({
			authorised: true,
			unauthorizedCheck: true,
			auth_token: access_token
		});
		const timetables = await get_timetables_id_db
			.get(`/timetable/${timetable_id}`)
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
		const put_timetable_id_db = create_ky_instance({
			authorised: true,
			unauthorizedCheck: true,
			auth_token: access_token
		}).extend({
			json: timetable_data
		});
		await put_timetable_id_db.put(`/timetable/${timetable_id}`);
		return Ok('');
	} catch (error) {
		return Err('Something went wrong ' + error);
	}
}

export async function delete_timetable_by_id(
	access_token: string,
	timetable_id: string
): Promise<Result<string, string>> {
	try {
		const delete_timetable_id_db = create_ky_instance({
			authorised: true,
			unauthorizedCheck: true,
			auth_token: access_token
		});
		await delete_timetable_id_db.delete(`/timetable/${timetable_id}`);
		return Ok('');
	} catch (error) {
		return Err('Something went wrong ' + error);
	}
}

export async function create_empty_timetable(
	access_token: string,
	timetable_name: string,
	semester: number,
	academic_year: string
): Promise<Result<TimetableInfo, string>> {
	const timetable_post_template: TimetablePostTemplate = {
		academicYear: academic_year,
		metaData: [],
		name: timetable_name,
		semester: semester
	};
	try {
		const create_empty_timetable_db = create_ky_instance({
			authorised: true,
			unauthorizedCheck: true,
			auth_token: access_token
		}).extend({
			json: timetable_post_template
		});
		const timetable_info = await create_empty_timetable_db.post('/timetable').json<TimetableInfo>();
		return Ok(timetable_info);
	} catch (error) {
		return Err('Something went wrong ' + error);
	}
}
