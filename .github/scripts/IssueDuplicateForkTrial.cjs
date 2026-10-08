const { gather, validate } = require('./IssueDuplicates.cjs');

async function getSourceContext(github, context, issueNumber) {
    if (
        context.repo.owner !== 'kubaflo' ||
        context.repo.repo !== 'maui' ||
        context.payload.repository.full_name !== 'kubaflo/maui' ||
        context.payload.repository.private !== false ||
        context.actor !== 'kubaflo' ||
        context.ref !== 'refs/heads/duplicate-detector-fork-trial-20261008' ||
        context.eventName !== 'workflow_dispatch' ||
        !['true', true].includes(context.payload.inputs?.staged) ||
        ![39229, 39220].includes(issueNumber) ||
        (context.payload.inputs?.aw_context ?? '') !== ''
    ) {
        throw new Error('Untrusted fork trial context.');
    }
    const repository = { owner: 'dotnet', repo: 'maui' };
    const { data } = await github.rest.repos.get(repository);
    if (
        data.full_name !== 'dotnet/maui' ||
        data.private !== false ||
        data.visibility !== 'public'
    ) {
        throw new Error('Fork trial evidence must come from public dotnet/maui.');
    }
    // Only the evidence context is rebound; execution remains on the checked fork/ref.
    return {
        ...context,
        repo: repository,
        ref: `refs/heads/${data.default_branch}`,
        payload: { ...context.payload, repository: data },
    };
}

module.exports = {
    async gather(options) {
        const context = await getSourceContext(
            options.github,
            options.context,
            options.issueNumber,
        );
        await gather({ ...options, context });
    },
    async validate(options) {
        if (options.staged !== true) throw new Error('Fork trials cannot publish comments.');
        const context = await getSourceContext(
            options.github,
            options.context,
            options.issueNumber,
        );
        await validate({
            ...options,
            context,
            staged: true,
            reportRepository: options.context.repo,
        });
    },
};
