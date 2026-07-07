<script lang="ts">
	import { onMount } from 'svelte';

	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { registered, token_information } from '$lib/shared/shared.svelte';
	import LoginButton from './LoginButton.svelte';

	let emailInput = $state('');
	let passwordInput = $state('');
	let errorMessage = $state('');
	onMount(() => {
		for (const [key, value] of new URLSearchParams(window.location.hash)) {
			if (key.includes('access_token')) {
				$token_information.a = value;
				goto(resolve('/home'));
			}

			if (key.includes('error_description')) {
				errorMessage = value;
			}
		}

		for (const [key, value] of new URLSearchParams(window.location.search)) {
			if (key.includes('error_description')) {
				errorMessage = value;
			}
		}

		registered.set(false);

		// if ($token_information.a !== '' || $token_information.b) {
		// 	console.log($token_information);
		// 	goto(resolve('/home'));
		// }
	});
</script>

<div>Login Here!</div>
<div class="text-error">{errorMessage}</div>
<form class="fieldset content-around px-4">
	<fieldset class="fieldset">
		<!-- svelte-ignore a11y_label_has_associated_control -->
		<label class="label">Email</label>
		<input
			type="email"
			class="validator input w-full"
			placeholder="Email"
			required
			bind:value={emailInput}
		/>
		<p class="validator-hint hidden">Required</p>
	</fieldset>

	<label class="fieldset">
		<span class="label">Password</span>
		<input
			type="password"
			class="validator input w-full"
			placeholder="Password"
			required
			bind:value={passwordInput}
		/>
		<span class="validator-hint hidden">Required</span>
	</label>

	<LoginButton email={emailInput} password={passwordInput} bind:errorMessage></LoginButton>
</form>
<div>
	Don't have an account yet? Register <button
		class="cursor-pointer underline"
		onclick={() => goto(resolve('/register'))}>here</button
	>
</div>
