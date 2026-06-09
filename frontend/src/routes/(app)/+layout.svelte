<script lang="ts">
	import type { LayoutProps } from './$types';
	import mods_tgt_header from '$lib/assets/mods_tgt_header.png?enhanced';
	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	let { data, children }: LayoutProps = $props();
</script>

<div class="navbar bg-base-200 shadow-sm">
	<!-- svelte-ignore a11y_missing_attribute -->
	<enhanced:img
		class="aspect-5/2 h-14 w-auto align-middle"
		src={mods_tgt_header}
		alt="Mods Together Logo"
	/>
</div>

<div class="px-2 pt-2">
	{@render children()}
</div>
