import { PUBLIC_DB_LINK } from '$env/static/public';

export async function login_to_db(username: string, password: string) {
	const url = `${PUBLIC_DB_LINK}/auth/login`;
	const options = {
		method: 'POST',
		headers: { 'content-type': 'application/json' },
		body: `{"email": "${username}", "password":"${password}"}`
	};

	console.log(options);

	try {
		const response = await fetch(url, options);
		const data = await response.json();
		console.log(data);
	} catch (error) {
		console.error(error);
	}
}
