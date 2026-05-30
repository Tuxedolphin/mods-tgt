<script lang="ts">
	import type { LayoutProps } from './$types';

	// eslint-disable-next-line @typescript-eslint/no-unused-vars
	let { data, children }: LayoutProps = $props();
</script>

<div class="navbar bg-base-100 shadow-sm">
	<a class="btn text-xl btn-ghost">Mods Together!</a>
</div>

<div class="p-4">
	{@render children()}
</div>
