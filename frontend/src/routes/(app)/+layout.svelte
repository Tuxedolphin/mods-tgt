<script lang="ts">
	import type { LayoutProps } from './$types';

	let { data, children }: LayoutProps = $props();
</script>

<div class="navbar bg-base-100 shadow-sm">
	<a class="btn text-xl btn-ghost">Mods Together!</a>
</div>
{@render children()}
