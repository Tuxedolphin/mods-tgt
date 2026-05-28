import { PUBLIC_DB_LINK } from '$env/static/public';
import ky, { HTTPError } from 'ky';
import type { AuthResponse } from '../types/db_raw_types';
import { Err, Ok, type Result } from 'ts-results';

const apiCalls = ky.create({
	baseUrl: PUBLIC_DB_LINK
});
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
		if (error instanceof HTTPError) {
			console.log(error.data);
		}
		return new Err('Wrong email or password');
	}
}
