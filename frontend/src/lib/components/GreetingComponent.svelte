<script lang="ts">
	import { goto } from '$app/navigation';
	import { resolve } from '$app/paths';
	import { access_token } from '$lib/shared/shared.svelte';
	import { get_user_info } from '$lib/utils/db_operations';
	import { onMount } from 'svelte';

	let username = $state('');
	onMount(async () => {
		const tt = await get_user_info($access_token.access_token);

		if (tt.isOk()) {
			if (!tt.value.username) {
				goto(resolve('/newuser'));
			} else {
				username = tt.value.username;
			}
		}
	});
</script>

<div>Welcome, {username}</div>
