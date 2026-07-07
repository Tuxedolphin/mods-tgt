<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import {
		currentUserInformation,
		currentWorkingTimetable,
		token_information
	} from '$lib/shared/shared.svelte';
	import { get_user_info, login_to_db } from '$lib/utils/db_operations';

	interface LoginButtonProps {
		email: string;
		password: string;
		errorMessage: string;
	}
	let loading = $state(false);
	// biome-ignore lint/correctness/noUnusedVariables: Unable to detect bindable variables
	let { errorMessage = $bindable<string>(), email, password }: LoginButtonProps = $props();

	async function login() {
		const urlParams = new URLSearchParams(window.location.search);
		loading = true;
		const result = await login_to_db(email, password);
		if (result.isOk()) {
			console.log('Ok result?');
			// Stores access token in localstorage (FOR NOW) -- Not secure:!
			$token_information.a = result.value.accessToken;
			$token_information.b = false;

			const tt = await get_user_info(result.value.accessToken);

			if (tt.isOk()) {
				if (!tt.value.username) {
					await goto(resolve('/newuser'));
				} else {
					$currentUserInformation.username = tt.value.username;
					$currentUserInformation.userId = tt.value.userId;
				}
			}

			if (urlParams.get('action') === 'redirect') {
				currentWorkingTimetable.reset();
				const tt_id = urlParams.get('tt_id')!;
				console.log('Redirect to: ' + tt_id);
				goto(
					resolve('/(app)/planner/[timetable_id]', {
						timetable_id: tt_id
					})
				);

				return;
			}

			goto(resolve('/home'));
		} else {
			errorMessage = result.error;
		}
		loading = false;
	}
</script>

{#if loading}
	<button class="btn btn-disabled mt-4 btn-neutral" disabled type="submit">Logging in...</button>
{:else}
	<button
		class="btn mt-4 btn-neutral"
		type="submit"
		onclick={async () => {
			await login();
		}}>Login</button
	>
{/if}
