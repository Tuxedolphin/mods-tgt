<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { token_information } from '$lib/shared/shared.svelte';
	import { login_to_db } from '$lib/utils/db_operations';

	interface LoginButtonProps {
		email: string;
		password: string;
	}
	let loading = $state(false);
	let { email, password }: LoginButtonProps = $props();
	let errorMessage = $state('');

	async function login() {
		loading = true;
		const result = await login_to_db(email, password);
		if (result.isOk()) {
			// Stores access token in localstorage (FOR NOW) -- Not secure:!
			$token_information.a = result.value.accessToken;
			$token_information.b = false;
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
	<button class="btn mt-4 btn-neutral" type="submit" onclick={() => login()}>Login</button>
{/if}

{#if errorMessage}
	{errorMessage}
{/if}
